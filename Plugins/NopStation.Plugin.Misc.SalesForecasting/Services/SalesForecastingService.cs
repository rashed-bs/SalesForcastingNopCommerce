using LinqToDB;
using Microsoft.ML;
using Microsoft.ML.Trainers.FastTree;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models;
using NopStation.Plugin.Misc.SalesForecasting.Models;
using LinqToDB.Common;
using Nop.Core.Caching;
using NopStation.Plugin.Misc.SalesForecasting.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualBasic;
using Nop.Services.Discounts;
using Nop.Services.Common;
using Nop.Core.Domain.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace NopStation.Plugin.Misc.SalesForecasting.Services
{
    
    public partial class SalesForecastingService : ISalesForecastingService
    {
        #region ctor
        private readonly IRepository<Order> _orderRepository;
            private readonly IRepository<ProductCategory> _productCategoryRepository;
            private readonly IRepository<OrderItem> _orderItemRepository;
            private readonly IRepository<Address> _addressRepository;
            private readonly IRepository<Product> _productRepository;
            private readonly IRepository<Category> _categoryRepository;
            private readonly IStaticCacheManager _staticCacheManager;
            private readonly INopFileProvider _fileProvider;
            private readonly ICategoryService _categoryService;
            private readonly IStoreContext _storeContext;
            private readonly IDiscountService _discountService;
            private readonly IProductService _productService;
            private readonly SalesForecastingSettings _inventoryPredictionSettings;
            private readonly ILogger _logger;
            private readonly MLContext _mlContext = new MLContext(seed: 0);

            public SalesForecastingService(
                IRepository<Order> orderRepository,
                IRepository<ProductCategory> productCategoryRepository,
                IRepository<OrderItem> orderItemRepository,
                IRepository<Product> productRepository,
                IStaticCacheManager staticCacheManager,
                IRepository<Category> categoryRepository,
                IDiscountService discountService,
                INopFileProvider fileProvider,
                ICategoryService categoryService,
                IStoreContext storeContext,
                IProductService productService,
                SalesForecastingSettings inventoryPredictionSettings,
                ILogger logger)
            {
                _orderRepository = orderRepository;
                _productCategoryRepository = productCategoryRepository;
                _orderItemRepository = orderItemRepository;
                _productRepository = productRepository;
                _staticCacheManager = staticCacheManager;
                _fileProvider = fileProvider;
                _categoryService = categoryService;
                _categoryRepository = categoryRepository;
                _categoryService = categoryService;
                _storeContext = storeContext;
                _productService = productService;
                _discountService = discountService;
                _inventoryPredictionSettings = inventoryPredictionSettings;
                _logger = logger;
            }

        #endregion

        #region Utilities
        public async Task<Dictionary<int, int>> CreateCategoryMappingSubToRoot()
        {
            var categoryMappingSubToRoot = new Dictionary<int, int>();
            var categoryInformationsQuery = from c in _categoryRepository.Table
                                            select new CategoryMappingSubToRoot
                                            {
                                                SubCategoryId = c.Id,
                                                RootId = c.ParentCategoryId
                                            };
            var categoryInformations = await categoryInformationsQuery.ToListAsync();

            // Initialize the parent (root) dictionary where each category is its own parent (root)
            var parent = new Dictionary<int, int>();
            foreach (var categoryInfo in categoryInformations)
            {
                parent[categoryInfo.SubCategoryId] = categoryInfo.SubCategoryId;
            }

            // Find the root of a category (with path compression)
            int findRoot(int categoryId)
            {
                if (parent[categoryId] != categoryId)
                {
                    parent[categoryId] = findRoot(parent[categoryId]); // Path compression
                }
                return parent[categoryId];
            }

            // Union operation to merge two disjoint sets
            void union(int category1, int category2)
            {
                var root1 = findRoot(category1);
                var root2 = findRoot(category2);

                if (root1 != root2)
                {
                    parent[root2] = root1;
                }
            }

            // Build the disjoint sets using the DSU algorithm
            foreach (var categoryInfo in categoryInformations)
            {
                var subCategory = categoryInfo.SubCategoryId;
                var rootCategory = categoryInfo.RootId;

                // If the category has no parent (rootId is 0), consider it as the root category
                if (rootCategory == 0)
                {
                    rootCategory = subCategory;
                }

                union(rootCategory, subCategory);
            }

            // Create the categoryMappingSubToRoot dictionary
            foreach (var categoryInfo in categoryInformations)
            {
                var subCategory = categoryInfo.SubCategoryId;
                var rootCategory = findRoot(subCategory);
                categoryMappingSubToRoot[subCategory] = rootCategory;
            }

            return categoryMappingSubToRoot;
        }
        public async Task<List<SalesData>> AddDiscountData(List<SalesData> dataset)
        {
            var allDiscounts = await _discountService.GetAllDiscountsAsync();
            var allDiscountsWithoutNull = allDiscounts.Where(x => x.StartDateUtc != null).ToList();
            var sortedDiscounts = allDiscountsWithoutNull.OrderBy(discount => discount.StartDateUtc.Value).ToList();

            int datasetPointer = 0;
            foreach(var discount in sortedDiscounts)
            {
                DateTime startDate = discount.StartDateUtc.Value;
                while (datasetPointer < dataset.Count && GetDateTime(dataset[datasetPointer]) < startDate)
                {
                    datasetPointer++;
                }

                while (datasetPointer < dataset.Count)
                {
                    dataset[datasetPointer].PercentageDiscount = float.Max((float)discount.DiscountPercentage, dataset[datasetPointer].PercentageDiscount); // setting the best discount for an order
                    datasetPointer++;
                }
            }
            return dataset;
        }
        private IQueryable<CategoryAvgPrice> CagegoryAvgPrices()
        {
            var query = from cp in (from oi in _orderItemRepository.Table
                                    join o in _orderRepository.Table on oi.OrderId equals o.Id
                                    join pc in _productCategoryRepository.Table on oi.ProductId equals pc.ProductId
                                    where o.OrderTotal != 0
                                    select new
                                    {
                                        CategoryId = pc.CategoryId,
                                        o.OrderTotal,
                                        Unit = oi.Quantity
                                    })
                        group cp by new { cp.CategoryId } into gcp
                        select new CategoryAvgPrice
                        {
                            CategoryId = gcp.Key.CategoryId,
                            AvgPrice = (float)gcp.Sum(x => x.OrderTotal) / gcp.Sum(x => x.Unit),
                        };
            return query;
        }
        private async Task<Dictionary<int, float>> RootCategoryAvgPrices()
        {
            // get category information
            var categorySubToRoot = await _staticCacheManager.GetAsync(SalesForecastingDefaults.CategorySubToRootMappingCacheKey, CreateCategoryMappingSubToRoot);

            var avgPriceMap = new Dictionary<int, float>();

            var query = from oi in _orderItemRepository.Table
                        join o in _orderRepository.Table on oi.OrderId equals o.Id
                        join pc in _productCategoryRepository.Table on oi.ProductId equals pc.ProductId
                        where o.OrderTotal != 0
                        select new TempCategoryAvgPrice
                        {
                            CategoryId = pc.CategoryId,
                            OrderTotal = (float)o.OrderTotal,
                            Unit = oi.Quantity
                        };

            var queryResult = await query.ToListAsync();
            queryResult.ForEach(x => x.CategoryId = categorySubToRoot[x.CategoryId]);
            var categoryAvgPriceInformations = queryResult.GroupBy(x => new { x.CategoryId })
                .Select(g => new CategoryAvgPrice
                {
                    CategoryId = g.Key.CategoryId,
                    AvgPrice = (float)g.Sum(x => x.OrderTotal) / g.Sum(x => x.Unit)
                });

            foreach (var  categoryAvgPrice in categoryAvgPriceInformations)
            {
                avgPriceMap[categoryAvgPrice.CategoryId] = categoryAvgPrice.AvgPrice;
            }

            return avgPriceMap;
        }
        private string CategoryGroupModelName(List<CategoryAvgPrice> categoryAvgPrices)
        {
            string categoryGroupName = "TimeSeriesSSa_Categories";
            foreach (var categoryAvgPrice in categoryAvgPrices)
            {
                categoryGroupName += "_" + categoryAvgPrice.CategoryId.ToString();
            }
            return categoryGroupName;
        }
        private static DateTime GetDateTime(SalesData salesData)
        {
            int year = (int)salesData.Year;
            int month = (int)salesData.Month;
            int day = (int)salesData.Day;
            return new DateTime(year, month, day);
        }
        private async Task<List<List<CategoryAvgPrice>>> GetCategoryGroups()
        {
            // Get the category average prices
            var categoryAvgPrices = await CagegoryAvgPrices().ToListAsync();

            // Sort categoryAvgPrices based on AvgPrice
            categoryAvgPrices.Sort((x, y) => x.AvgPrice.CompareTo(y.AvgPrice));

            // Group categories based on the difference in average prices
            var ThresholdPrice = 50;
            var categoryGroups = new List<List<CategoryAvgPrice>>();
            var currentGroup = new List<CategoryAvgPrice> { categoryAvgPrices[0] };

            for (int i = 1; i < categoryAvgPrices.Count; i++)
            {
                var currentCategory = categoryAvgPrices[i];
                var previousCategory = categoryAvgPrices[i - 1];
                var priceDifference = Math.Abs(currentCategory.AvgPrice - previousCategory.AvgPrice);

                if (priceDifference > ThresholdPrice)
                {
                    // Create a new group when the difference is greater than the threshold
                    categoryGroups.Add(currentGroup);
                    currentGroup = new List<CategoryAvgPrice>();
                }

                currentGroup.Add(currentCategory);
            }

            // Add the last group to the list
            categoryGroups.Add(currentGroup);

            return categoryGroups;
        }
        private async Task<List<SalesData>> WeeklySalesHistoryQuery(List<int> categories)
        {
            var weekends = new List<int> { 5, 6 }; // Saturday and Sunday

            var query = from sd in (
                            from fsd in (
                                from si in (
                                    from oi in _orderItemRepository.Table
                                    join o in _orderRepository.Table on oi.OrderId equals o.Id
                                    join pc in _productCategoryRepository.Table on oi.ProductId equals pc.ProductId
                                    where o.OrderTotal != 0
                                    select new
                                    {
                                        OrderDate = o.CreatedOnUtc.Date,
                                        CategoryId = pc.CategoryId,
                                        Discount = 0,
                                        o.OrderTotal,
                                        ShippingCharge = o.OrderShippingExclTax,
                                        Unit = oi.Quantity
                                    }
                                )
                                group si by new { si.OrderDate, si.OrderDate.Year, si.OrderDate.Month, si.OrderDate.Day } into gsi
                                orderby gsi.Key.OrderDate
                                select new
                                {
                                    OrderDate = gsi.Key.OrderDate,
                                    Year = gsi.Key.OrderDate.Year,
                                    Month = gsi.Key.OrderDate.Month,
                                    Day = gsi.Key.OrderDate.Day,
                                    CategoryID = gsi.Max(x => x.CategoryId),
                                    PercentageDiscount = 0,
                                    ShippingCharge = (float)gsi.Max(x => x.ShippingCharge),
                                    UnitsSold = gsi.Sum(x => x.Unit)
                                }
                            )
                            where categories.Contains(fsd.CategoryID)
                            select new
                            {
                                OrderDate = fsd.OrderDate,
                                Year = fsd.Year,
                                Month = fsd.Month,
                                Day = fsd.Day,
                                CategoryID = fsd.CategoryID,
                                ShippingCharge = fsd.ShippingCharge,
                                PercentageDiscount = 0,
                                PreviousDayUnits = Sql.Ext.Lag(fsd.UnitsSold, Sql.Nulls.Respect, 1, 0).Over().OrderBy(fsd.OrderDate.Date).ToValue(),
                                NextDayUnits = Sql.Ext.Lead(fsd.UnitsSold, Sql.Nulls.Respect, 1, 0).Over().OrderBy(fsd.OrderDate.Date).ToValue(),
                            }
                        )
                        select new SalesData
                        {
                            CategoryID = sd.CategoryID,
                            Year = sd.Year,
                            Day = sd.Day,
                            Month = sd.Month,
                            IsWeekend = weekends.Contains((int)sd.OrderDate.DayOfWeek) ? (float)1.0 : (float)0.0,
                            NextDayUnits = sd.NextDayUnits, 
                            PreviousDayUnits = sd.PreviousDayUnits, 
                            PercentageDiscount = 0, 
                            ShippingCharge = sd.ShippingCharge
                        };

            var salesData = await query.ToListAsync();

            if (salesData.Count > 14)
            {
                //return FillMissingValues(salesData, weekends);
                DateTime startDate = GetDateTime(salesData[0]);
                DateTime endDate = GetDateTime(salesData[salesData.Count-1]);
                var dataRecordPossible = endDate.Subtract(startDate).Days;
                var dataRecordAvaiable = salesData.Count;
                var percentageOfDataAvaiable = ((float)dataRecordAvaiable / (float)dataRecordPossible) * 100;

                // use interpolation if the avaiable data above 80%
                if (percentageOfDataAvaiable > 80)
                {
                    return FillMissingValuesByMovingAverage(salesData, weekends);
                }
                else
                {
                    var enrichedData = FillMissingValuesByCherryPick(salesData, weekends);
                    dataRecordAvaiable = enrichedData.Count;
                    percentageOfDataAvaiable = ((float)dataRecordAvaiable / (float)dataRecordPossible) * 100;
                    return FillMissingValuesByMovingAverage(enrichedData, weekends);
                }
            }
            else
                return salesData;
        }
        private async Task<List<ProductData>> ProductSalesHistoryQuery()
        {
            var weekends = new List<int> { 5, 6 }; // Saturday and Sunday

            var query = from fcoi in (from coi in (from oi in _orderItemRepository.Table
                                                   join o in _orderRepository.Table on oi.OrderId equals o.Id
                                                   join pc in _productCategoryRepository.Table on oi.ProductId equals pc.ProductId
                                                   where o.OrderTotal != 0
                                                   select new
                                                   {
                                                       OrderDate = o.CreatedOnUtc.Date,
                                                       CategoryId = pc.CategoryId,
                                                       o.OrderTotal,
                                                       o.BillingAddressId,
                                                       Unit = oi.Quantity,
                                                   })
                                      group coi by new { coi.OrderDate, coi.CategoryId, coi.BillingAddressId } into gcoi
                                      select new
                                      {
                                          OrderDate = gcoi.Key.OrderDate,
                                          CategoryId = gcoi.Key.CategoryId,
                                          OrderTotal = gcoi.Sum(o => o.OrderTotal),
                                          Units = gcoi.Sum(o => o.Unit),
                                          BillingAddressId = gcoi.Key.BillingAddressId,
                                      })
                        select new ProductData
                        {
                            CategoryAvgPrice = 0,
                            DiscountRate = 0,
                            Month = fcoi.OrderDate.Month,
                            Day = fcoi.OrderDate.Day,
                            CategoryId = fcoi.CategoryId,
                            CountryId = fcoi.CategoryId,
                            IsWeekend = weekends.Contains(fcoi.OrderDate.Day) ? 1 : 0, 
                            Units = fcoi.Units // this is to predict
                        };
            return await query.ToListAsync();
        }
        private async Task<List<CategoryBaseModelData>> CategoryBaseModelSalesHistory()
        {
            var categorySubToRoot = await _staticCacheManager.GetAsync(SalesForecastingDefaults.CategorySubToRootMappingCacheKey, CreateCategoryMappingSubToRoot);

            var categoryAvgPrices = await _staticCacheManager.GetAsync(SalesForecastingDefaults.RootCategoryAvgPricesCacheKey, RootCategoryAvgPrices);

            var query = from oi in _orderItemRepository.Table
                        join o in _orderRepository.Table on oi.OrderId equals o.Id
                        join pc in _productCategoryRepository.Table on oi.ProductId equals pc.ProductId
                        where o.OrderTotal != 0
                        select new CategoryBaseTempModelData
                        {
                            Year = o.CreatedOnUtc.Date.Year,
                            Month = o.CreatedOnUtc.Date.Month,
                            CategoryId = pc.CategoryId,
                            OrderTotal = (float)o.OrderTotal,
                            Unit = oi.Quantity
                        };
            var queryResult = await query.ToListAsync();

            queryResult.ForEach(x => x.CategoryId = categorySubToRoot[(int)x.CategoryId]);

            var groupedResult = queryResult
                .GroupBy(x => new { x.Year, x.Month, x.CategoryId })
                .Select(g => new CategoryBaseTempModelData
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    CategoryId = g.Key.CategoryId,
                    Avg = g.Sum(x => x.Unit) / g.Count(),
                    Max = g.Max(x => x.Unit),
                    Min = g.Min(x => x.Unit),
                    CategoryAvgPrice = categoryAvgPrices[(int)g.Key.CategoryId],
                    OrderTotal = g.Sum(x => x.OrderTotal),
                    Unit = g.Sum(x => x.Unit)
                })
                .ToList();
            var finalResut = groupedResult.Select((x, index) => new CategoryBaseModelData
            {
                Year = (int)x.Year,
                Month = (int)x.Month,
                Avg = (float)x.Avg, 
                Max = (float)x.Max,
                Min = (float)x.Min,
                CategoryAvgPrice = (float)x.CategoryAvgPrice, 
                CategoryId = (int)x.CategoryId, 
                UnitsSoldCurrent = (float)x.Unit, 
                UnitsSoldPrev = index > 0 ? (float)groupedResult[index - 1].Unit : 0, 
                Next = index < groupedResult.Count - 1 ? (float)groupedResult[index + 1].Unit : 0
            });
            return finalResut.ToList();
        }

        #endregion

        #region Missing Values Utilities 
        // Cubic interpolation method
        private static double CubicInterpolate(double x, double x0, double x1, double y0, double y1, double m0, double m1)
        {
            double t = (x - x0) / (x1 - x0);
            double t2 = t * t;
            double t3 = t2 * t;
            double a = 2 * t3 - 3 * t2 + 1;
            double b = t3 - 2 * t2 + t;
            double c = -2 * t3 + 3 * t2;
            double d = t3 - t2;
            return a * y0 + b * m0 * (x1 - x0) + c * y1 + d * m1 * (x1 - x0);
        }

        // Calculate the first derivatives (slopes) at each data point using central differences
        private static double[] CalculateSlopesNextValue(List<SalesData> dataset)
        {
            int n = dataset.Count;
            double[] slopes = new double[n];

            for (int i = 1; i < n - 1; i++)
            {
                double h1 = GetDateTime(dataset[i]).Subtract(GetDateTime(dataset[i - 1])).Days;
                double h2 = GetDateTime(dataset[i + 1]).Subtract(GetDateTime(dataset[i])).Days;
                double y1 = dataset[i - 1].NextDayUnits;
                double y2 = dataset[i].NextDayUnits;
                double y3 = dataset[i + 1].NextDayUnits;

                slopes[i] = (h2 * (y1 - y2) + h1 * (y2 - y3)) / (h1 * h2 * (h1 + h2));
            }

            // Extrapolate the first and last slopes
            slopes[0] = 2 * slopes[1] - slopes[2];
            slopes[n - 1] = 2 * slopes[n - 2] - slopes[n - 3];

            return slopes;
        }

        // Calculate the first derivatives (slopes) at each data point using central differences
        private static double[] CalculateSlopesPrevValue(List<SalesData> dataset)
        {
            int n = dataset.Count;
            double[] slopes = new double[n];

            for (int i = 1; i < n - 1; i++)
            {
                double h1 = GetDateTime(dataset[i]).Subtract(GetDateTime(dataset[i - 1])).Days;
                double h2 = GetDateTime(dataset[i + 1]).Subtract(GetDateTime(dataset[i])).Days;
                double y1 = dataset[i - 1].PreviousDayUnits;
                double y2 = dataset[i].PreviousDayUnits;
                double y3 = dataset[i + 1].PreviousDayUnits;

                slopes[i] = (h2 * (y1 - y2) + h1 * (y2 - y3)) / (h1 * h2 * (h1 + h2));
            }

            // Extrapolate the first and last slopes
            slopes[0] = 2 * slopes[1] - slopes[2];
            slopes[n - 1] = 2 * slopes[n - 2] - slopes[n - 3];

            return slopes;
        }

        // Perform cubic interpolation to fill in missing values in the dataset
        public static List<SalesData> FillMissingValuesByIterpolation(List<SalesData> dataset, List<int> weekends)
        {
            var filledDataset = new List<SalesData>();

            double[] slopesNextValue = CalculateSlopesNextValue(dataset);
            double[] slopesPrevValue = CalculateSlopesPrevValue(dataset);

            for (int i = 0; i < dataset.Count; i++)
            {
                filledDataset.Add(dataset[i]);

                // Check if there's a missing value between two consecutive data points
                if (i < dataset.Count - 1 && GetDateTime(dataset[i + 1]).Subtract(GetDateTime(dataset[i])).Days > 1)
                {
                    DateTime startDate = GetDateTime(dataset[i]);
                    DateTime endDate = GetDateTime(dataset[i + 1]);

                    double startValueNextValue = dataset[i].NextDayUnits;
                    double endValueNextValue = dataset[i + 1].NextDayUnits;
                    double startSlopeNextValue = slopesNextValue[i];
                    double endSlopeNextValue = slopesNextValue[i + 1];

                    double startValuePrevValue = dataset[i].PreviousDayUnits;
                    double endValuePrevValue = dataset[i + 1].PreviousDayUnits;
                    double startSlopePrevValue = slopesPrevValue[i];
                    double endSlopePrevValue = slopesPrevValue[i + 1];

                    // Calculate the number of missing days between the two data points
                    int missingDays = (int)(endDate - startDate).TotalDays - 1;

                    // Perform cubic interpolation for the missing values
                    for (int j = 1; j <= missingDays; j++)
                    {
                        DateTime missingDate = startDate.AddDays(j);
                        double interpolatedNextValue = CubicInterpolate((int)missingDate.DayOfWeek, (int)startDate.DayOfWeek, (int)endDate.DayOfWeek, startValueNextValue, endValueNextValue, startSlopeNextValue, endSlopeNextValue);

                        double interpolatedPrevValue = CubicInterpolate((int)missingDate.DayOfWeek, (int)startDate.DayOfWeek, (int)endDate.DayOfWeek, startValuePrevValue, endValuePrevValue, startSlopePrevValue, endSlopePrevValue);

                        // Add the interpolated data point to the filled dataset
                        filledDataset.Add(new SalesData
                        {
                            Year = missingDate.Year,
                            Month = missingDate.Month,
                            Day = missingDate.Day,
                            CategoryID = dataset[i].CategoryID,
                            IsWeekend = weekends.Contains((int)missingDate.DayOfWeek) ? (float)1.0 : (float)0.0,
                            ShippingCharge = dataset[i].ShippingCharge,
                            PercentageDiscount = dataset[i].PercentageDiscount,
                            PreviousDayUnits = float.Max((float)interpolatedPrevValue, 0),
                            NextDayUnits = float.Max((float)interpolatedNextValue, 0),
                        });
                    }
                }
            }

            return filledDataset;
        }

        // Perform cherrypick method for filling in the missing values in dataset
        public static List<SalesData> FillMissingValuesByCherryPick(List<SalesData> dataset, List<int> weekends)
        {
            var filledDataset = new List<SalesData>();
            var prevNextUnitsCache = new Dictionary<string, Tuple<float, float>>(); // Cache previous and next units

            foreach(var eachSale  in dataset)
            {
                string key = $"{eachSale.Month}_{eachSale.Day}";
                prevNextUnitsCache[key] = new Tuple<float,float>(eachSale.PreviousDayUnits, eachSale.NextDayUnits);
            }

            for (int i = 0; i < dataset.Count; i++)
            {
                filledDataset.Add(dataset[i]);

                // Check if there's a missing value between two consecutive data points
                if (i < dataset.Count - 1 && GetDateTime(dataset[i + 1]).Subtract(GetDateTime(dataset[i])).Days > 1)
                {
                    DateTime startDate = GetDateTime(dataset[i]);
                    DateTime endDate = GetDateTime(dataset[i + 1]);

                    // Calculate the number of missing days between the two data points
                    int missingDays = (int)(endDate - startDate).TotalDays - 1;

                    // Perform cubic interpolation for the missing values
                    for (int j = 1; j <= missingDays; j++)
                    {
                        DateTime missingDate = startDate.AddDays(j);
                        string key = $"{missingDate.Month}_{missingDate.Day}";
                        float cherryPickedPrevValue = 0;
                        float cherryPickedNextValue = 0;

                        if(prevNextUnitsCache.ContainsKey(key))
                        {
                            cherryPickedPrevValue = prevNextUnitsCache[key].Item1;
                            cherryPickedNextValue = prevNextUnitsCache[key].Item2;
                        }

                        // Add the interpolated data point to the filled dataset
                        filledDataset.Add(new SalesData
                        {
                            Year = missingDate.Year,
                            Month = missingDate.Month,
                            Day = missingDate.Day,
                            CategoryID = dataset[i].CategoryID,
                            IsWeekend = weekends.Contains((int)missingDate.DayOfWeek) ? (float)1.0 : (float)0.0,
                            ShippingCharge = dataset[i].ShippingCharge,
                            PercentageDiscount = dataset[i].PercentageDiscount,
                            PreviousDayUnits = cherryPickedPrevValue,
                            NextDayUnits = cherryPickedNextValue
                        });
                    }
                }
            }

            return filledDataset;
        }

        // Perform movingAverage or windowing method for filling in the missing values in dataset
        public static List<SalesData> FillMissingValuesByMovingAverage(List<SalesData> dataset, List<int> weekends)
        {
            var filledDataset = new List<SalesData>();

            for (int i = 0; i < dataset.Count; i++)
            {
                filledDataset.Add(dataset[i]);

                // Check if there's a missing value between two consecutive data points
                if (i < dataset.Count - 1 && GetDateTime(dataset[i + 1]).Subtract(GetDateTime(dataset[i])).Days > 1)
                {
                    DateTime startDate = GetDateTime(dataset[i]);
                    DateTime endDate = GetDateTime(dataset[i + 1]);

                    // Calculate the number of missing days between the two data points
                    int missingDays = (int)(endDate - startDate).TotalDays - 1;

                    // Perform cubic interpolation for the missing values
                    for (int j = 1; j <= missingDays; j++)
                    {
                        #region moving average calculation
                        DateTime missingDate = startDate.AddDays(j);
                        float movingAvgPrev = 0;
                        float movingAvgNext = 0;

                        if (filledDataset.Count == 1)
                        {
                            movingAvgPrev = filledDataset[0].PreviousDayUnits;
                            movingAvgNext = filledDataset[0].NextDayUnits;
                        }
                        else if (filledDataset.Count == 2)
                        {
                            movingAvgPrev = (filledDataset[0].PreviousDayUnits + filledDataset[1].PreviousDayUnits) / 2;
                            movingAvgNext = (filledDataset[0].NextDayUnits + filledDataset[1].NextDayUnits) / 2;
                        }
                        else if (filledDataset.Count > 2)
                        {
                            movingAvgPrev = (filledDataset[0].PreviousDayUnits + filledDataset[1].PreviousDayUnits + filledDataset[2].PreviousDayUnits) / 3;
                            movingAvgNext = (filledDataset[0].NextDayUnits + filledDataset[1].NextDayUnits + filledDataset[2].NextDayUnits) / 3;
                        }
                        #endregion

                        // Add the interpolated data point to the filled dataset
                        filledDataset.Add(new SalesData
                        {
                            Year = missingDate.Year,
                            Month = missingDate.Month,
                            Day = missingDate.Day,
                            CategoryID = dataset[i].CategoryID,
                            IsWeekend = weekends.Contains((int)missingDate.DayOfWeek) ? (float)1.0 : (float)0.0,
                            ShippingCharge = dataset[i].ShippingCharge,
                            PercentageDiscount = dataset[i].PercentageDiscount,
                            PreviousDayUnits = movingAvgPrev,
                            NextDayUnits = movingAvgNext,
                        });
                    }
                }
            }

            return filledDataset;
        }
        #endregion

        #region Time Series Methods
        public virtual async Task<(bool, string)> TrainWeeklySalesPredictionModelAsync(bool logInfo = false)
        {
            var sucess = false;

            // get the category groups 
            var categoryGroups = await GetCategoryGroups();
            
            // save the group in cache
            await _staticCacheManager.SetAsync(_staticCacheManager.PrepareKeyForDefaultCache(SalesForecastingDefaults.CategoryGroupsCacheKey), categoryGroups);

            // clean up the directories 
            var mLModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelPathTimeSeries);
            if (_fileProvider.DirectoryExists(mLModelPath))
                _fileProvider.DeleteDirectory(mLModelPath);
            _fileProvider.CreateDirectory(mLModelPath);

            var mlMonthlyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelMonthlyPredictionPathTimeSeries);
            if (_fileProvider.DirectoryExists(mlMonthlyModelPath))
                _fileProvider.DeleteDirectory(mlMonthlyModelPath);
            _fileProvider.CreateDirectory(mlMonthlyModelPath);

            var mlWeeklyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelWeeklyPredictionPathTimeSeries);
            if (_fileProvider.DirectoryExists(mlWeeklyModelPath))
                _fileProvider.DeleteDirectory(mlWeeklyModelPath);
            _fileProvider.CreateDirectory(mlWeeklyModelPath);

            foreach (var categoryGroup in categoryGroups)
            {
                // get data for each category group 
                List<int> categories = new();
                foreach(var item in categoryGroup)
                {
                    categories.Add(item.CategoryId);
                }
                // get sales data
                var salesTrainHistory = await WeeklySalesHistoryQuery(categories);
                var modelName = CategoryGroupModelName(categoryGroup);
                var outputPathToSaveModel = _fileProvider.Combine(mlWeeklyModelPath, modelName);
                if(salesTrainHistory.Count() > 14)
                {
                    // add the discount data to it 
                    salesTrainHistory = await AddDiscountData(salesTrainHistory);
                    ForecastingModelHelper.TrainAndSaveTimeSeriesSalesForecastingModelWeekly(_mlContext, salesTrainHistory, outputPathToSaveModel);
                }
            }

            sucess = true;

            return (sucess, string.Empty);
        }
        #endregion

        #region Regression Methods (Large feature space)
        public async Task<(bool, string)> TrainLargeFeatureProductSalesPredictionModelAsync(bool logInfo = false)
        {
            var sucess = false;

            var productSalesHistory = await ProductSalesHistoryQuery();

            //var productHistory = await GetProductHistoryDataAsync();

            // clean up the directories 
            var mLModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelPathRegression);
            if (_fileProvider.DirectoryExists(mLModelPath))
                _fileProvider.DeleteDirectory(mLModelPath);
            _fileProvider.CreateDirectory(mLModelPath);

            var mlMonthlyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelMonthlyPredictionPathRegression);
            if (_fileProvider.DirectoryExists(mlMonthlyModelPath))
                _fileProvider.DeleteDirectory(mlMonthlyModelPath);
            _fileProvider.CreateDirectory(mlMonthlyModelPath);

            var mlWeeklyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelWeeklyPredictionPathRegression);
            if (_fileProvider.DirectoryExists(mlWeeklyModelPath))
                _fileProvider.DeleteDirectory(mlWeeklyModelPath);
            _fileProvider.CreateDirectory(mlWeeklyModelPath);

            var outputPathToSaveModel = _fileProvider.Combine(mlWeeklyModelPath, SalesForecastingDefaults.RegressionProductSalesFastTreeWeedieModelName);

            ForecastingModelHelper.TrainAndSaveLargeFeatureRegressionProductSalesForecastingModel(_mlContext, productSalesHistory, outputPathToSaveModel);

            sucess = true;

            return (sucess, string.Empty);
        }
        #endregion

        #region Base Category Model Methods
        public async Task<(bool, string)> TrainBaseCategoryWiseProductSalesPredictionModelAsync(bool logInfo = false)
        {
            var sucess = false;

            var productSalesHistory = await CategoryBaseModelSalesHistory();

            // clean up the directories 
            var mLModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelPathEnsemble);
            if (_fileProvider.DirectoryExists(mLModelPath))
                _fileProvider.DeleteDirectory(mLModelPath);
            _fileProvider.CreateDirectory(mLModelPath);

            var mlMonthlyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelMonthlyPredictionPathEnsemble);
            if (_fileProvider.DirectoryExists(mlMonthlyModelPath))
                _fileProvider.DeleteDirectory(mlMonthlyModelPath);
            _fileProvider.CreateDirectory(mlMonthlyModelPath);

            var mlWeeklyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelWeeklyPredictionPathEnsemble);
            if (_fileProvider.DirectoryExists(mlWeeklyModelPath))
                _fileProvider.DeleteDirectory(mlWeeklyModelPath);
            _fileProvider.CreateDirectory(mlWeeklyModelPath);

            var outputPathToSaveModel = _fileProvider.Combine(mlMonthlyModelPath, SalesForecastingDefaults.ProductSalesCategoryWiseBaseModel);

            ForecastingModelHelper.TrainAndSaveCategoryWiseBaseModel(_mlContext, productSalesHistory, outputPathToSaveModel);

            sucess = true;

            return (sucess, string.Empty);
        }

        #endregion

        #region Base Location Model Methods
        public async Task<(bool, string)> TrainBaseLocationWiseProductSalesPredictionModelAsync(bool logInfo = false)
        {
            var sucess = false;

            var productSalesHistory = await ProductSalesHistoryQuery();

            //var productHistory = await GetProductHistoryDataAsync();

            // clean up the directories 
            var mLModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelPathRegression);
            if (_fileProvider.DirectoryExists(mLModelPath))
                _fileProvider.DeleteDirectory(mLModelPath);
            _fileProvider.CreateDirectory(mLModelPath);

            var mlMonthlyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelMonthlyPredictionPathRegression);
            if (_fileProvider.DirectoryExists(mlMonthlyModelPath))
                _fileProvider.DeleteDirectory(mlMonthlyModelPath);
            _fileProvider.CreateDirectory(mlMonthlyModelPath);

            var mlWeeklyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelWeeklyPredictionPathRegression);
            if (_fileProvider.DirectoryExists(mlWeeklyModelPath))
                _fileProvider.DeleteDirectory(mlWeeklyModelPath);
            _fileProvider.CreateDirectory(mlWeeklyModelPath);

            var outputPathToSaveModel = _fileProvider.Combine(mlWeeklyModelPath, SalesForecastingDefaults.RegressionProductSalesFastTreeWeedieModelName);

            ForecastingModelHelper.TrainAndSaveLargeFeatureRegressionProductSalesForecastingModel(_mlContext, productSalesHistory, outputPathToSaveModel);

            sucess = true;

            return (sucess, string.Empty);
        }

        #endregion

        #region Ensemble Model Methods
        public async Task<(bool, string)> TrainEnsembleMetaModelAsync(bool logInfo = false)
        {
            bool success = false;

            return (success, string.Empty);
        }

        #endregion
    }
}
