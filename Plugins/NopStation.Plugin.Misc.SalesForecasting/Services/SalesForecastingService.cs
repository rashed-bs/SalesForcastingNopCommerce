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
using NopStation.Plugin.Misc.SalesForecasting.Models.DataPreprocessingModels;
using NopStation.Plugin.Misc.SalesForecasting.Helpers;
using NUglify.Helpers;
using System.Runtime.Intrinsics.X86;
using DocumentFormat.OpenXml.Bibliography;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            IRepository<Address> addressRepository,
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
            _addressRepository = addressRepository;
            _discountService = discountService;
            _inventoryPredictionSettings = inventoryPredictionSettings;
            _logger = logger;
        }

        #endregion

        #region Utilities
        public float LoadMetricFromFile(string metricFilePath)
        {
            if (File.Exists(metricFilePath))
            {
                string metricString = File.ReadAllText(metricFilePath);
                if (float.TryParse(metricString, out float metric))
                {
                    return metric;
                }
            }

            // Return a default value or handle error cases
            return 0.0f;
        }
        public void PathPreparation()
        {
            #region time series
            var mLModelPathTime = _fileProvider.MapPath(SalesForecastingDefaults.MLModelPathTimeSeries);
            if (_fileProvider.DirectoryExists(mLModelPathTime))
                _fileProvider.DeleteDirectory(mLModelPathTime);
            _fileProvider.CreateDirectory(mLModelPathTime);
            
            var mLModelPathInd = _fileProvider.MapPath(SalesForecastingDefaults.MLModelPathIndividualProductForcasting);
            if (_fileProvider.DirectoryExists(mLModelPathInd))
                _fileProvider.DeleteDirectory(mLModelPathInd);
            _fileProvider.CreateDirectory(mLModelPathInd);

            var mlMonthlyModelPathTime = _fileProvider.MapPath(SalesForecastingDefaults.MLModelMonthlyPredictionPathTimeSeries);
            if (_fileProvider.DirectoryExists(mlMonthlyModelPathTime))
                _fileProvider.DeleteDirectory(mlMonthlyModelPathTime);
            _fileProvider.CreateDirectory(mlMonthlyModelPathTime);

            var mlWeeklyModelPathTime = _fileProvider.MapPath(SalesForecastingDefaults.MLModelWeeklyPredictionPathTimeSeries);
            if (_fileProvider.DirectoryExists(mlWeeklyModelPathTime))
                _fileProvider.DeleteDirectory(mlWeeklyModelPathTime);
            _fileProvider.CreateDirectory(mlWeeklyModelPathTime);
            #endregion

            #region Regression
            var mLModelPathRegression = _fileProvider.MapPath(SalesForecastingDefaults.MLModelPathRegression);
            if (_fileProvider.DirectoryExists(mLModelPathRegression))
                _fileProvider.DeleteDirectory(mLModelPathRegression);
            _fileProvider.CreateDirectory(mLModelPathRegression);

            var mlMonthlyModelPathRegression = _fileProvider.MapPath(SalesForecastingDefaults.MLModelMonthlyPredictionPathRegression);
            if (_fileProvider.DirectoryExists(mlMonthlyModelPathRegression))
                _fileProvider.DeleteDirectory(mlMonthlyModelPathRegression);
            _fileProvider.CreateDirectory(mlMonthlyModelPathRegression);

            var mlWeeklyModelPathRegression = _fileProvider.MapPath(SalesForecastingDefaults.MLModelWeeklyPredictionPathRegression);
            if (_fileProvider.DirectoryExists(mlWeeklyModelPathRegression))
                _fileProvider.DeleteDirectory(mlWeeklyModelPathRegression);
            _fileProvider.CreateDirectory(mlWeeklyModelPathRegression);
            #endregion

            #region Ensemble
            var mLModelPathEnsemble = _fileProvider.MapPath(SalesForecastingDefaults.MLModelPathEnsemble);
            if (_fileProvider.DirectoryExists(mLModelPathEnsemble))
                _fileProvider.DeleteDirectory(mLModelPathEnsemble);
            _fileProvider.CreateDirectory(mLModelPathEnsemble);

            var mlMonthlyModelPathEnsemble = _fileProvider.MapPath(SalesForecastingDefaults.MLModelMonthlyPredictionPathEnsemble);
            if (_fileProvider.DirectoryExists(mlMonthlyModelPathEnsemble))
                _fileProvider.DeleteDirectory(mlMonthlyModelPathEnsemble);
            _fileProvider.CreateDirectory(mlMonthlyModelPathEnsemble);

            var mlWeeklyModelPathEnsemble = _fileProvider.MapPath(SalesForecastingDefaults.MLModelWeeklyPredictionPathEnsemble);
            if (_fileProvider.DirectoryExists(mlWeeklyModelPathEnsemble))
                _fileProvider.DeleteDirectory(mlWeeklyModelPathEnsemble);
            _fileProvider.CreateDirectory(mlWeeklyModelPathEnsemble);

            #endregion
        }
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
            foreach (var discount in sortedDiscounts)
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

            foreach (var categoryAvgPrice in categoryAvgPriceInformations)
            {
                avgPriceMap[categoryAvgPrice.CategoryId] = categoryAvgPrice.AvgPrice;
            }

            return avgPriceMap;
        }
        private async Task<List<CategoryTotalSelling>> TopSellingCategories()
        {
            var categorySubToRoot = await _staticCacheManager.GetAsync(SalesForecastingDefaults.CategorySubToRootMappingCacheKey, CreateCategoryMappingSubToRoot);
            var query = from oi in _orderItemRepository.Table
                        join o in _orderRepository.Table on oi.OrderId equals o.Id
                        join pc in _productCategoryRepository.Table on oi.ProductId equals pc.ProductId
                        where o.OrderTotal != 0
                        select new CategoryTotalTempSelling
                        {
                            CategoryId = pc.CategoryId,
                            OrderTotal = (float)o.OrderTotal,
                            OrderDate = o.CreatedOnUtc.Date
                        };

            var queryResult = await query.ToListAsync();
            queryResult.ForEach(x => x.CategoryId = categorySubToRoot[x.CategoryId]);
            var monthlyCategoryWiseSales = queryResult.GroupBy(x => new { x.OrderDate.Year, x.OrderDate.Month, x.CategoryId })
                .Select(g => new CategoryTotalSelling
                {
                    CategoryId = g.Key.CategoryId,
                    CumulativeOrderTotal = (float)g.Sum(x => x.OrderTotal)
                });
            var categoryAvgSalesPerMonth = monthlyCategoryWiseSales.GroupBy(x => new { x.CategoryId })
                .Select(g => new CategoryTotalSelling
                {
                    CategoryId = g.Key.CategoryId,
                    CumulativeOrderTotal = g.Sum(x => x.CumulativeOrderTotal) / g.Count()
                }).ToList();
            categoryAvgSalesPerMonth.Sort((a, b) => {
                if (a.CumulativeOrderTotal >= b.CumulativeOrderTotal)
                    return 1;
                else
                    return 0;
            });
            var userPreferedBestCategoryCount = 5;
            return categoryAvgSalesPerMonth.Take(userPreferedBestCategoryCount).ToList();
        }
        private async Task<List<LocationSpecificData>> TopOrderingLocations()
        {
            var query = from c in (from a in _orderRepository.Table
                                   join b in _addressRepository.Table on a.BillingAddressId equals b.Id
                                   select new
                                   {
                                       a.OrderTotal,
                                       b.CountryId
                                   })
                        group c by new { c.CountryId } into gc
                        select new LocationSpecificData
                        {
                            CountryId = (int)gc.Key.CountryId,
                            LocationAvgSales = (float)gc.Sum(x => x.OrderTotal) / gc.Count()
                        };
            var queryResult = await query.ToListAsync();
            queryResult.Sort((a, b) => b.LocationAvgSales.CompareTo(a.LocationAvgSales));
            var userPredefinedTopLocations = 5;
            return queryResult.Take(userPredefinedTopLocations).ToList();
        }
        private static DateTime GetDateTime(SalesData salesData)
        {
            int year = (int)salesData.Year;
            int month = (int)salesData.Month;
            int day = (int)salesData.Day;
            return new DateTime(year, month, day);
        }
        private static DateTime GetDateTime(ProductDailyGroupedTrainData productDailyGroupedTrainData)
        {
            int year = (int)productDailyGroupedTrainData.Year;
            int month = (int)productDailyGroupedTrainData.Month;
            int day = (int)productDailyGroupedTrainData.Day;
            return new DateTime(year, month, day);
        }
        private async Task<List<List<CategoryAvgPrice>>> GetCategoryGroups()
        {
            // Get the category average prices
            var categoryAvgPrices = await CagegoryAvgPrices().ToListAsync();

            // Sort categoryAvgPrices based on AvgPrice
            categoryAvgPrices.Sort((x, y) => x.AvgPrice.CompareTo(y.AvgPrice));

            // Group categories based on the difference in average prices
            var thresholdPrice = 1000;
            var categoryGroups = new List<List<CategoryAvgPrice>>();
            var currentGroup = new List<CategoryAvgPrice> { categoryAvgPrices[0] };

            for (int i = 1; i < categoryAvgPrices.Count; i++)
            {
                var currentCategory = categoryAvgPrices[i];
                var previousCategory = categoryAvgPrices[i - 1];
                var priceDifference = Math.Abs(currentCategory.AvgPrice - previousCategory.AvgPrice);

                if (priceDifference > thresholdPrice)
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
        private async Task<List<SalesData>> DailySalesHistoryQuery(List<int> categories)
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
                DateTime endDate = GetDateTime(salesData[salesData.Count - 1]);
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
        private async Task<List<SalesData>> DailySalesHistoryQueryLatestFirst()
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
                            orderby fsd.OrderDate descending
                            select new
                            {
                                OrderDate = fsd.OrderDate,
                                Year = fsd.Year,
                                Month = fsd.Month,
                                Day = fsd.Day,
                                CategoryID = fsd.CategoryID,
                                ShippingCharge = fsd.ShippingCharge,
                                PercentageDiscount = 0,
                                PreviousDayUnits = fsd.UnitsSold,
                                NextDayUnits = fsd.UnitsSold
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
        private async Task<List<CategoryBaseModelInputData>> CategoryBaseModelSalesHistory()
        {
            var categorySubToRoot = await _staticCacheManager.GetAsync(SalesForecastingDefaults.CategorySubToRootMappingCacheKey, CreateCategoryMappingSubToRoot);

            var categoryAvgPrices = await _staticCacheManager.GetAsync(SalesForecastingDefaults.RootCategoryAvgPricesCacheKey, RootCategoryAvgPrices);

            var topSellingCategories = await _staticCacheManager.GetAsync(SalesForecastingDefaults.TopSellingCategoriesCacheKey, TopSellingCategories);

            var bestCategories = topSellingCategories.Select(c => c.CategoryId).ToList();

            var query = from oi in _orderItemRepository.Table
                        join o in _orderRepository.Table on oi.OrderId equals o.Id
                        join pc in _productCategoryRepository.Table on oi.ProductId equals pc.ProductId
                        where o.OrderTotal != 0
                        select new TemporaryBaseModelData
                        {
                            Year = o.CreatedOnUtc.Date.Year,
                            Month = o.CreatedOnUtc.Date.Month,
                            CategoryId = pc.CategoryId,
                            OrderTotal = (float)o.OrderTotal,
                            Unit = oi.Quantity
                        };
            var queryResult = await query.ToListAsync();

            queryResult.ForEach(x => x.CategoryId = categorySubToRoot[(int)x.CategoryId]);

            var categoryWiseFilteredResult = queryResult.Where(x => bestCategories.Contains((int)x.CategoryId));

            var categoryWiseMonthlySales = categoryWiseFilteredResult.GroupBy(x => new { x.Year, x.Month, x.CategoryId })
                .Select(g => new TemporaryBaseModelData
                {
                    CategoryId = (int)g.Key.CategoryId,
                    Unit = g.Sum(x => x.Unit)
                }).ToList().OrderBy(x => x.CategoryId).ToList();
            var categoryWiseMonthlySalesWithLabel = categoryWiseMonthlySales.Select((x, index) => new CategoryBaseModelInputData
            {
                CategoryId = (int)x.CategoryId,
                UnitsSoldCurrent = (float)x.Unit,
                UnitsSoldPrev = (index > 0 && categoryWiseMonthlySales[index - 1].CategoryId == x.CategoryId) ? (float)categoryWiseMonthlySales[index - 1].Unit : 0,
                Next = (index < categoryWiseMonthlySales.Count - 1 && categoryWiseMonthlySales[index + 1].CategoryId == x.CategoryId) ? (float)categoryWiseMonthlySales[index + 1].Unit : 0
            }).ToList();

            MiscHelpers.Shuffle(categoryWiseMonthlySalesWithLabel);

            return categoryWiseMonthlySalesWithLabel;
        }
        private async Task<List<CategoryAvgPriceBaseModelInputData>> CategoryAvgPriceBaseModelSalesHistory()
        {
            var categorySubToRoot = await _staticCacheManager.GetAsync(SalesForecastingDefaults.CategorySubToRootMappingCacheKey, CreateCategoryMappingSubToRoot);

            var categoryAvgPrices = await _staticCacheManager.GetAsync(SalesForecastingDefaults.RootCategoryAvgPricesCacheKey, RootCategoryAvgPrices);

            var topSellingCategories = await _staticCacheManager.GetAsync(SalesForecastingDefaults.TopSellingCategoriesCacheKey, TopSellingCategories);

            var bestCategories = topSellingCategories.Select(c => c.CategoryId).ToList();

            var query = from oi in _orderItemRepository.Table
                        join o in _orderRepository.Table on oi.OrderId equals o.Id
                        join pc in _productCategoryRepository.Table on oi.ProductId equals pc.ProductId
                        where o.OrderTotal != 0
                        select new TemporaryBaseModelData
                        {
                            Year = o.CreatedOnUtc.Date.Year,
                            Month = o.CreatedOnUtc.Date.Month,
                            CategoryId = pc.CategoryId,
                            OrderTotal = (float)o.OrderTotal,
                            Unit = oi.Quantity
                        };
            var queryResult = await query.ToListAsync();

            queryResult.ForEach(x => x.CategoryId = categorySubToRoot[(int)x.CategoryId]);

            var categoryWiseFilteredResult = queryResult.Where(x => bestCategories.Contains((int)x.CategoryId));

            var categoryWiseMonthlySales = categoryWiseFilteredResult.GroupBy(x => new { x.Year, x.Month, x.CategoryId })
                .Select(g => new TemporaryBaseModelData
                {
                    CategoryId = (int)g.Key.CategoryId,
                    Unit = g.Sum(x => x.Unit)
                }).ToList().OrderBy(x => x.CategoryId).ToList();
            var categoryWiseMonthlySalesWithLabel = categoryWiseMonthlySales.Select((x, index) => new CategoryAvgPriceBaseModelInputData
            {
                CategoryId = (int)x.CategoryId,
                CategoryAvgPrice = categoryAvgPrices[(int)x.CategoryId],
                UnitsSoldCurrent = (float)x.Unit,
                UnitsSoldPrev = (index > 0 && categoryWiseMonthlySales[index - 1].CategoryId == x.CategoryId) ? (float)categoryWiseMonthlySales[index - 1].Unit : 0,
                Next = (index < categoryWiseMonthlySales.Count - 1 && categoryWiseMonthlySales[index + 1].CategoryId == x.CategoryId) ? (float)categoryWiseMonthlySales[index + 1].Unit : 0
            }).ToList();

            MiscHelpers.Shuffle(categoryWiseMonthlySalesWithLabel);

            return categoryWiseMonthlySalesWithLabel;
        }
        private async Task<List<LocationBaseModelInputData>> LocationBaseModelSalesHistory()
        {
            var topOrderingLocations = await _staticCacheManager.GetAsync(SalesForecastingDefaults.TopOrderingLocationsCacheKey, TopOrderingLocations);

            var bestLocations = topOrderingLocations.Select(c => c.CountryId).ToList();

            var topSellingCategories = await _staticCacheManager.GetAsync(SalesForecastingDefaults.TopSellingCategoriesCacheKey, TopSellingCategories);

            var bestCategories = topSellingCategories.Select(c => c.CategoryId).ToList();

            var query = from oi in _orderItemRepository.Table
                        join o in _orderRepository.Table on oi.OrderId equals o.Id
                        join pc in _productCategoryRepository.Table on oi.ProductId equals pc.ProductId
                        join ad in _addressRepository.Table on o.BillingAddressId equals ad.Id
                        where o.OrderTotal != 0
                        select new TemporaryBaseModelData
                        {
                            CategoryId = pc.CategoryId,
                            Year = o.CreatedOnUtc.Date.Year,
                            Month = o.CreatedOnUtc.Date.Month,
                            CountryId = ad.CountryId,
                            OrderTotal = (float)o.OrderTotal,
                            Unit = oi.Quantity
                        };
            var queryResult = await query.ToListAsync();
            var locationWiseFilteredResult = queryResult.Where(x => bestCategories.Contains((int)x.CategoryId) && bestLocations.Contains((int)x.CountryId)).ToList();
            var locationWiseMonthlySales = locationWiseFilteredResult.GroupBy(x => new { x.Year, x.Month, x.CountryId })
                .Select(g => new TemporaryBaseModelData
                {
                    CountryId = (int)g.Key.CountryId,
                    Unit = g.Sum(x => x.Unit)
                }).ToList().OrderBy(x => x.CategoryId).ToList();
            var locationWiseMonthlySalesWithLabel = locationWiseMonthlySales.Select((x, index) => new LocationBaseModelInputData
            {
                CountryId = (int)x.CountryId,
                UnitsSoldCurrent = (float)x.Unit,
                UnitsSoldPrev = (index > 0 && locationWiseMonthlySales[index - 1].CountryId == x.CountryId) ? (float)locationWiseMonthlySales[index - 1].Unit : 0,
                Next = (index < locationWiseMonthlySales.Count - 1 && locationWiseMonthlySales[index + 1].CountryId == x.CountryId) ? (float)locationWiseMonthlySales[index + 1].Unit : 0
            }).ToList();


            MiscHelpers.Shuffle(locationWiseMonthlySalesWithLabel);

            return locationWiseMonthlySalesWithLabel;
        }
        private async Task<List<MonthBaseModelInputData>> MonthBaseModelSalesHistory()
        {
            var query = from goi in (from oi in _orderItemRepository.Table
                                     join o in _orderRepository.Table on oi.OrderId equals o.Id
                                     where o.OrderTotal != 0
                                     select new TemporaryBaseModelData
                                     {
                                         Year = o.CreatedOnUtc.Date.Year,
                                         Month = o.CreatedOnUtc.Date.Month,
                                         OrderTotal = (float)o.OrderTotal,
                                         Unit = oi.Quantity
                                     })
                        group goi by new { goi.Year, goi.Month } into ggoi
                        select new MonthBaseModelInputData
                        {
                            Month = (int)ggoi.Key.Month,
                            UnitsSoldCurrent = (float)ggoi.Sum(x => x.Unit)
                        };
            var queryResult = await query.ToListAsync();
            var monthWiseMonthlySalesWithLabel = queryResult.Select((x, index) => new MonthBaseModelInputData
            {
                Month = x.Month,
                UnitsSoldCurrent = x.UnitsSoldCurrent,
                UnitsSoldPrev = index > 0 ? (float)queryResult[index - 1].UnitsSoldCurrent : 0,
                Next = index < queryResult.Count - 1 ? (float)queryResult[index + 1].UnitsSoldCurrent : 0
            }).ToList();

            MiscHelpers.Shuffle(monthWiseMonthlySalesWithLabel);

            return monthWiseMonthlySalesWithLabel;
        }
        private async Task<List<MonthBaseModelInputData>> MonthBaseSalesHistoryPrevYear()
        {
            var query1 = from goi in (from oi in _orderItemRepository.Table
                                      join o in _orderRepository.Table on oi.OrderId equals o.Id
                                      where o.OrderTotal != 0
                                      select new TemporaryBaseModelData
                                      {
                                          Year = o.CreatedOnUtc.Date.Year,
                                          Month = o.CreatedOnUtc.Date.Month,
                                          OrderTotal = (float)o.OrderTotal,
                                          Unit = oi.Quantity
                                      })
                         group goi by new { goi.Year, goi.Month } into ggoi
                         orderby ggoi.Key.Year descending, ggoi.Key.Month ascending
                         select new
                         {
                             Year = ggoi.Key.Year,
                             Month = (int)ggoi.Key.Month,
                             UnitsSoldCurrent = (float)ggoi.Sum(x => x.Unit)
                         };
            var res = await query1.ToListAsync();

            var query = from goi in (from oi in _orderItemRepository.Table
                                     join o in _orderRepository.Table on oi.OrderId equals o.Id
                                     where o.OrderTotal != 0
                                     select new TemporaryBaseModelData
                                     {
                                         Year = o.CreatedOnUtc.Date.Year,
                                         Month = o.CreatedOnUtc.Date.Month,
                                         OrderTotal = (float)o.OrderTotal,
                                         Unit = oi.Quantity
                                     })
                        group goi by new { goi.Year, goi.Month } into ggoi
                        orderby ggoi.Key.Year descending, ggoi.Key.Month descending
                        select new MonthBaseModelInputData
                        {
                            Month = (int)ggoi.Key.Month,
                            UnitsSoldCurrent = (float)ggoi.Sum(x => x.Unit)
                        };
            var queryResult = await query.ToListAsync();
            var last12Entries = queryResult.Take(12).ToList();
            return last12Entries;
        }

        #endregion

        #region Controller Helpers
        public async Task<List<SalesData>> DailySalesHistoryQueryLastMonth()
        {
            var dailySalesData = await DailySalesHistoryQueryLatestFirst();
            return dailySalesData.Take(30).ToList();
        }
        #endregion

        #region Ensemble Model Training Data Preparation Utilities

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

            foreach (var eachSale in dataset)
            {
                string key = $"{eachSale.Month}_{eachSale.Day}";
                prevNextUnitsCache[key] = new Tuple<float, float>(eachSale.PreviousDayUnits, eachSale.NextDayUnits);
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

                        if (prevNextUnitsCache.ContainsKey(key))
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
        public virtual async Task<(bool, string)> TrainWeeklySalesPredictionTimeSeriesModelAsync(bool logInfo = false)
        {
            var sucess = false;

            // get the category groups 
            var categoryGroups = await GetCategoryGroups();

            // save the group in cache
            await _staticCacheManager.SetAsync(_staticCacheManager.PrepareKeyForDefaultCache(SalesForecastingDefaults.CategoryGroupsCacheKey), categoryGroups);

            var mlWeeklyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelWeeklyPredictionPathTimeSeries);
            foreach (var categoryGroup in categoryGroups)
            {
                // get data for each category group 
                List<int> categories = new();
                foreach (var item in categoryGroup)
                {
                    categories.Add(item.CategoryId);
                }
                // get sales data
                var salesTrainHistory = await DailySalesHistoryQuery(categories);
                var modelName = $"{categories[0].ToString()}" + "timeSeriesModel.zip";
                var outputPathToSaveModel = _fileProvider.Combine(mlWeeklyModelPath, modelName);
                if (salesTrainHistory.Count() > 14)
                {
                    // add the discount data to it 
                    salesTrainHistory = await AddDiscountData(salesTrainHistory);
                    ForecastingModelHelper.TrainAndSaveTimeSeriesSalesForecastingModelWeekly(_mlContext, salesTrainHistory, outputPathToSaveModel);
                }
            }

            sucess = true;

            return (sucess, string.Empty);
        }

        public List<float> TimeSeriesPredictWeeklySales(bool logInfo = false)
        {
            var mlWeeklyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelWeeklyPredictionPathTimeSeries);
            var modelFiles = Directory.GetFiles(mlWeeklyModelPath);
            var allTheCategoryWiseModelNames = new List<string>();
            allTheCategoryWiseModelNames.AddRange(modelFiles);

            // get 7 days prediction for every category wise model
            var modelPredictions = new List<WeeklySalesTimeSeriesPrediction>();
            foreach (var modelFilePath in modelFiles)
            {
                var predictions = ForecastingModelHelper.TimeSeriesSalesForecastingPredictionWeekly(_mlContext, modelFilePath);
                modelPredictions.Add(predictions);
            }
            var finalPredictionsFromAlltheCategories = new List<float>();
            for (int i = 0; i < 7; i++)
            {
                finalPredictionsFromAlltheCategories.Add(0);
            }
            foreach (var predictions in modelPredictions)
            {
                for (int i = 0; i < predictions.ForecastedWeeklySalesUnits.Count(); i++)
                {
                    finalPredictionsFromAlltheCategories[i] += float.Max(0, predictions.ForecastedWeeklySalesUnits[i]);
                }
            }
            return finalPredictionsFromAlltheCategories;
        }
        #endregion

        #region Regression Methods (Large feature space)
        public async Task<(bool, string)> TrainLargeFeatureProductSalesPredictionModelAsync(bool logInfo = false)
        {
            var sucess = false;

            var productSalesHistory = await ProductSalesHistoryQuery();

            var mlWeeklyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelWeeklyPredictionPathRegression);
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

            var mlMonthlyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelMonthlyPredictionPathEnsemble);
            var outputPathToSaveModel = _fileProvider.Combine(mlMonthlyModelPath, SalesForecastingDefaults.ProductSalesCategoryWiseBaseModel);

            // save the training data to a csv file 
            CsvWriter.WriteToCsv(outputPathToSaveModel + "trainingData.csv", productSalesHistory);

            var metric = ForecastingModelHelper.TrainAndSaveCategoryWiseBaseModel(_mlContext, productSalesHistory, outputPathToSaveModel);

            // save the metric 
            var metricFilePath = outputPathToSaveModel + "metric.txt";
            File.WriteAllText(metricFilePath, metric.ToString());

            sucess = true;

            return (sucess, string.Empty);
        }

        #endregion

        #region Base Location Model Methods
        public async Task<(bool, string)> TrainBaseLocationWiseProductSalesPredictionModelAsync(bool logInfo = false)
        {
            var sucess = false;

            var productSalesHistory = await LocationBaseModelSalesHistory();

            var mlMonthlyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelMonthlyPredictionPathEnsemble);
            var outputPathToSaveModel = _fileProvider.Combine(mlMonthlyModelPath, SalesForecastingDefaults.ProductSalesLocationWiseBaseModel);

            // save the training data to a csv file 
            CsvWriter.WriteToCsv(outputPathToSaveModel + "trainingData.csv", productSalesHistory);

            var metric = ForecastingModelHelper.TrainAndSaveLocationWiseBaseModel(_mlContext, productSalesHistory, outputPathToSaveModel);

            // save the metric 
            var metricFilePath = outputPathToSaveModel + "metric.txt";
            File.WriteAllText(metricFilePath, metric.ToString());


            sucess = true;

            return (sucess, string.Empty);
        }

        #endregion

        #region Base Category and Category Avg.Prices Model Methods
        public async Task<(bool, string)> TrainBaseCategoryAvgPriceWiseProductSalesPredictionModelAsync(bool logInfo = false)
        {
            var sucess = false;

            var productSalesHistory = await CategoryAvgPriceBaseModelSalesHistory();

            var mlMonthlyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelMonthlyPredictionPathEnsemble);
            var outputPathToSaveModel = _fileProvider.Combine(mlMonthlyModelPath, SalesForecastingDefaults.ProductSalesCategoryAvgPriceWiseBaseModel);

            // save the training data to a csv file 
            CsvWriter.WriteToCsv(outputPathToSaveModel + "trainingData.csv", productSalesHistory);

            var metric = ForecastingModelHelper.TrainAndSaveAveragePriceWiseBaseModel(_mlContext, productSalesHistory, outputPathToSaveModel);

            // save the metric 
            var metricFilePath = outputPathToSaveModel + "metric.txt";
            File.WriteAllText(metricFilePath, metric.ToString());


            sucess = true;

            return (sucess, string.Empty);
        }

        #endregion

        #region Base Month Wise Model Methods
        public async Task<(bool, string)> TrainBaseMonthWiseProductSalesPredictionModelAsync(bool logInfo = false)
        {
            var sucess = false;

            var productSalesHistory = await MonthBaseModelSalesHistory();

            var mlMonthlyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelMonthlyPredictionPathEnsemble);
            var outputPathToSaveModel = _fileProvider.Combine(mlMonthlyModelPath, SalesForecastingDefaults.ProductSalesMonthWiseBaseModel);

            // save the training data to a csv file 
            CsvWriter.WriteToCsv(outputPathToSaveModel + "trainingData.csv", productSalesHistory);

            var metric = ForecastingModelHelper.TrainAndSaveMonthWiseBaseModel(_mlContext, productSalesHistory, outputPathToSaveModel);

            // save the metric 
            var metricFilePath = outputPathToSaveModel + "metric.txt";
            File.WriteAllText(metricFilePath, metric.ToString());


            sucess = true;

            return (sucess, string.Empty);
        }
        #endregion

        #region Ensemble Prediction Methods
        public async Task<List<MonthBaseModelInputData>> MonthlySalesHistoryQueryLastYear()
        {
            var last12MonthSalesData = await MonthBaseSalesHistoryPrevYear();
            return last12MonthSalesData;
        }
        public async Task<float> PredictEnsembleNextMonthSales()
        {
            #region Data
            // get month base prev year sales history 
            var prevYearMonthlySalesHistory = await MonthBaseSalesHistoryPrevYear();
            var unitSoldPrev = prevYearMonthlySalesHistory.Count >= 2 ? prevYearMonthlySalesHistory[1].UnitsSoldCurrent : 0;
            var unitSoldCurrent = prevYearMonthlySalesHistory[0].UnitsSoldCurrent;

            #endregion

            #region category wise prediction
            // get top categories 
            var topCategories = await _staticCacheManager.GetAsync(SalesForecastingDefaults.TopSellingCategoriesCacheKey, TopSellingCategories);

            // get prediction from category wise base model (for each top category)
            var mlMonthlyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelMonthlyPredictionPathEnsemble);
            var outputPathToLoadCategoryWiseModel = _fileProvider.Combine(mlMonthlyModelPath, SalesForecastingDefaults.ProductSalesCategoryWiseBaseModel);

            var categoryCumulativePrediction = 0.0;
            foreach (var catetory in topCategories)
            {
                var predictedSalesCategoryWise = ForecastingModelHelper.PredictCategoryWiseBaseModel(_mlContext, outputPathToLoadCategoryWiseModel, new CategoryBaseModelInputData
                {
                    CategoryId = catetory.CategoryId,
                    UnitsSoldPrev = unitSoldPrev,
                    UnitsSoldCurrent = unitSoldCurrent,
                    Next = 0
                });
                categoryCumulativePrediction += predictedSalesCategoryWise;
            }
            #endregion

            #region avg price prediction
            // get prediction from category avg. price wise base model (for each top category)
            var categoryAvgPrices = await _staticCacheManager.GetAsync(SalesForecastingDefaults.RootCategoryAvgPricesCacheKey, RootCategoryAvgPrices);
            var categoryAvgPriceCumulativePrediction = 0.0;
            var outputPathToLoadCategoryAvgPriceWiseModel = _fileProvider.Combine(mlMonthlyModelPath, SalesForecastingDefaults.ProductSalesCategoryAvgPriceWiseBaseModel);
            foreach (var catetory in topCategories)
            {
                var predictedSalesAvgPriceWise = ForecastingModelHelper.PredictCategoryAvgPriceWiseBaseModel(_mlContext, outputPathToLoadCategoryAvgPriceWiseModel, new CategoryAvgPriceBaseModelInputData
                {
                    CategoryId = catetory.CategoryId,
                    CategoryAvgPrice = categoryAvgPrices.ContainsKey(catetory.CategoryId) ? categoryAvgPrices[catetory.CategoryId] : 0,
                    UnitsSoldCurrent = unitSoldCurrent,
                    UnitsSoldPrev = unitSoldPrev,
                    Next = 0
                });
                categoryAvgPriceCumulativePrediction += predictedSalesAvgPriceWise;
            }
            #endregion

            #region month wise prediction 
            // get prediction from month wise base model 
            var outputPathToLoadMonthWiseModel = _fileProvider.Combine(mlMonthlyModelPath, SalesForecastingDefaults.ProductSalesMonthWiseBaseModel);
            var monthWisePrediction = ForecastingModelHelper.PredictMonthWiseBaseModel(_mlContext, outputPathToLoadMonthWiseModel, new MonthBaseModelInputData
            {
                Month = DateTime.UtcNow.Month + 1,
                UnitsSoldCurrent = unitSoldCurrent,
                UnitsSoldPrev = unitSoldPrev,
                Next = 0
            });
            #endregion

            #region location wise prediction
            // get prediction from location wise base model (for each top location)
            var outputPathToLoadLocationWiseModel = _fileProvider.Combine(mlMonthlyModelPath, SalesForecastingDefaults.ProductSalesLocationWiseBaseModel);
            var topOrderingLocations = await _staticCacheManager.GetAsync(SalesForecastingDefaults.TopOrderingLocationsCacheKey, TopOrderingLocations);
            var bestLocations = topOrderingLocations.Select(c => c.CountryId).ToList();
            var locationCumulativePrediction = 0.0;
            foreach (var location in bestLocations)
            {
                var locationWisePrediction = ForecastingModelHelper.PredictLocationWiseBaseModel(_mlContext, outputPathToLoadLocationWiseModel, new LocationBaseModelInputData
                {
                    CountryId = location,
                    UnitsSoldCurrent = unitSoldCurrent,
                    UnitsSoldPrev = unitSoldPrev,
                    Next = 0
                });
                locationCumulativePrediction += locationWisePrediction;
            }
            #endregion

            #region Mathematical modeling for base model accuracy normalization
            // load the accuracy metrics from file 
            // make mathematical reasoning model and give priority (0 - 1.0) scale 
            var categoryAccuracy = 0.0;
            string categoryAccuracyText = File.ReadAllText(outputPathToLoadCategoryWiseModel + "metric.txt");
            if (float.TryParse(categoryAccuracyText, out float catVal))
            {
                categoryAccuracy = float.Max(catVal, (float)categoryAccuracy);
            }

            var avgPriceAccuracy = 0.0;
            string categoryAvgPriceAccuracyText = File.ReadAllText(outputPathToLoadCategoryAvgPriceWiseModel + "metric.txt");
            if (float.TryParse(categoryAvgPriceAccuracyText, out float avgVal))
            {
                avgPriceAccuracy = float.Max(avgVal, (float)avgPriceAccuracy);
            }

            var monthAccuracy = 0.0;
            string monthAccuracyText = File.ReadAllText(outputPathToLoadMonthWiseModel + "metric.txt");
            if (float.TryParse(monthAccuracyText, out float monVal))
            {
                monthAccuracy = float.Max(monVal, (float)monthAccuracy);
            }

            var locationAccuracy = 0.0;
            string locationAccuracyText = File.ReadAllText(outputPathToLoadLocationWiseModel + "metric.txt");
            if (float.TryParse(locationAccuracyText, out float locVal))
            {
                locationAccuracy = float.Max(locVal, (float)locationAccuracy);
            }

            var cumulativeAccuracy = categoryAccuracy + avgPriceAccuracy + monthAccuracy + locationAccuracy;
            var normalizedCategoryAccuracy = categoryAccuracy / cumulativeAccuracy;
            var normalizedAvgPriceAccuracy = avgPriceAccuracy / cumulativeAccuracy;
            var normalizedMonthAccuracy = monthAccuracy / cumulativeAccuracy;
            var normalizedLocationAccuracy = locationAccuracy / cumulativeAccuracy;

            #endregion

            #region Prediction
            var prediction = categoryCumulativePrediction * normalizedCategoryAccuracy + categoryAvgPriceCumulativePrediction * normalizedAvgPriceAccuracy + monthWisePrediction * normalizedMonthAccuracy + locationCumulativePrediction * normalizedLocationAccuracy;

            return (float)prediction;
            #endregion
        }
        public async Task<List<MonthlySalesCategoryContribution>> PredictCategorySalesContribution()
        {
            #region Data
            // get month base prev year sales history 
            var prevYearMonthlySalesHistory = await MonthBaseSalesHistoryPrevYear();
            var unitSoldPrev = prevYearMonthlySalesHistory.Count >= 2 ? prevYearMonthlySalesHistory[1].UnitsSoldCurrent : 0;
            var unitSoldCurrent = prevYearMonthlySalesHistory[0].UnitsSoldCurrent;

            #endregion

            // get top categories 
            var topCategories = await _staticCacheManager.GetAsync(SalesForecastingDefaults.TopSellingCategoriesCacheKey, TopSellingCategories);
            var bestCategories = topCategories.Select(x => x.CategoryId).ToList();
            // Get CategoryId to CategoryName mapping
            var query = from a in _categoryRepository.Table
                        select new
                        {
                            Id = a.Id,
                            Name = a.Name
                        };
            var queyResult = await query.ToListAsync();
            var filteredQueryResult = queyResult
                .Where(x => bestCategories.Contains(x.Id))
                .Select(x => new
                {
                    Id = x.Id,
                    Name = x.Name
                });

            // get prediction from category wise base model (for each top category)
            var mlMonthlyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelMonthlyPredictionPathEnsemble);
            var outputPathToLoadCategoryWiseModel = _fileProvider.Combine(mlMonthlyModelPath, SalesForecastingDefaults.ProductSalesCategoryWiseBaseModel);

            var contributionList = new List<MonthlySalesCategoryContribution>();
            var categoryCumulativePrediction = 0.0;
            foreach (var catetory in filteredQueryResult)
            {
                var predictedSalesCategoryWise = ForecastingModelHelper.PredictCategoryWiseBaseModel(_mlContext, outputPathToLoadCategoryWiseModel, new CategoryBaseModelInputData
                {
                    CategoryId = catetory.Id,
                    UnitsSoldPrev = unitSoldPrev,
                    UnitsSoldCurrent = unitSoldCurrent,
                    Next = 0
                });
                categoryCumulativePrediction += predictedSalesCategoryWise;
                contributionList.Add(new MonthlySalesCategoryContribution
                {
                    CategoryId = catetory.Id,
                    CategoryName = catetory.Name,
                    quantity = predictedSalesCategoryWise,
                    contribution = 0
                });
            }
            foreach (var contribution in contributionList)
            {
                contribution.contribution = ((float)contribution.quantity / (float)categoryCumulativePrediction) * 100;
            }
            return contributionList;
        }

        public async Task<Dictionary<int, int>> GetStocksOfBestCategories()
        {
            var stockQuantity = new Dictionary<int,int>();
            // get top categories 
            var topCategories = await _staticCacheManager.GetAsync(SalesForecastingDefaults.TopSellingCategoriesCacheKey, TopSellingCategories);
            var bestCategories = topCategories.Select(x => x.CategoryId).ToList();
            var categorySubToRoot = await _staticCacheManager.GetAsync(SalesForecastingDefaults.CategorySubToRootMappingCacheKey, CreateCategoryMappingSubToRoot);

            var query = from pc in _productCategoryRepository.Table
                        select pc;

            var productIDtoCategoryIds = await query.ToListAsync();
            productIDtoCategoryIds.ForEach(x => x.CategoryId = categorySubToRoot[x.CategoryId]);

            var finalProductIds = productIDtoCategoryIds.Where(x => bestCategories.Contains(x.CategoryId))
                .ToList();
            
            foreach(var p_id in finalProductIds)
            {
                var stock = await (from p in _productRepository.Table
                                   where p.Id == p_id.ProductId
                                   select p.StockQuantity).ToListAsync();
                if(stock.Count > 0)
                {
                    if(stockQuantity.ContainsKey(p_id.CategoryId))
                    {
                        stockQuantity[p_id.CategoryId] += stock[0];
                    } else
                    {
                        stockQuantity[p_id.CategoryId] = stock[0];
                    }
                } else
                {
                    continue;
                }
            }

            return stockQuantity;
        }

        #endregion

        #region Individual Product Sales Prediction

        #region Data Preparation
        private List<TempProductWeeklyTrainData> HandleMissingValueForIndividualProductWeeklyTrainData(List<TempProductWeeklyTrainData> productWeeklyTrainDatas)
        {
            // sort 
            productWeeklyTrainDatas = productWeeklyTrainDatas
                .OrderBy(data => data.Year)
                .ThenBy(data => data.Month)
                .ThenBy(data => data.Week)
                .ToList();

            var resolvedData = new List<TempProductWeeklyTrainData>();
            var trainDataCache = new Dictionary<Tuple<int, int>, float>();
            foreach(var data in productWeeklyTrainDatas)
            {
                trainDataCache[new Tuple<int, int>((int)data.Month, (int)data.Week)] = data.CurrentWeekQuantity;
            }

            if(productWeeklyTrainDatas.Count <= 0)
                return new List<TempProductWeeklyTrainData>();

            resolvedData.Add(productWeeklyTrainDatas[0]);
            for (int i = 1; i < productWeeklyTrainDatas.Count; i++)
            {
                if (resolvedData[resolvedData.Count - 1].Month != productWeeklyTrainDatas[i].Month)
                {
                    resolvedData.Add(productWeeklyTrainDatas[i]);
                } else if (resolvedData[resolvedData.Count - 1].Month == productWeeklyTrainDatas[i].Month && (productWeeklyTrainDatas[i].Week - resolvedData[resolvedData.Count - 1].Week) == 1)
                {
                    resolvedData.Add(productWeeklyTrainDatas[i]);
                } else
                {
                    int missingWeeks = (int)productWeeklyTrainDatas[i].Week - (int)resolvedData[resolvedData.Count - 1].Week;
                    for(int j = 1;j < missingWeeks; j++)
                    {
                        int missingWeek =  (int)resolvedData[resolvedData.Count - 1].Week + j;
                        if(trainDataCache.ContainsKey(new Tuple<int, int>((int)productWeeklyTrainDatas[i].Month, (int)productWeeklyTrainDatas[i].Week)))
                        {
                            resolvedData.Add(new TempProductWeeklyTrainData()
                            {
                                Year = productWeeklyTrainDatas[i].Year,
                                Month = productWeeklyTrainDatas[i].Month,
                                Week = missingWeek,
                                CurrentWeekQuantity = trainDataCache[new Tuple<int, int>((int)productWeeklyTrainDatas[i].Month, (int)productWeeklyTrainDatas[i].Week)],
                            });
                        }
                    }
                    resolvedData.Add(productWeeklyTrainDatas[i] );
                }
            }
            return resolvedData;
        }

        private List<ProductMonthlyTrainData> HandleMissingValueForIndividualProductMonthlyTrainData(List<ProductMonthlyTrainData> productMonthlyTrainDatas)
        {
            var resolvedData = new List<ProductMonthlyTrainData>();
            var trainDataCache = new Dictionary<int, ProductMonthlyTrainData>();
            foreach (var data in productMonthlyTrainDatas)
            {
                trainDataCache[(int)data.Month] = data;
            }

            if (productMonthlyTrainDatas.Count <= 0)
                return new List<ProductMonthlyTrainData>();

            resolvedData.Add(productMonthlyTrainDatas[0]);
            for (int i = 1; i < productMonthlyTrainDatas.Count; i++)
            {
                var lastMonth = resolvedData[resolvedData.Count - 1].Month;
                if(lastMonth + 1 == productMonthlyTrainDatas[i].Month)
                {
                    resolvedData.Add(productMonthlyTrainDatas[i]);
                } 
                else if(lastMonth < productMonthlyTrainDatas[i].Month)
                {
                    var missingMonths = productMonthlyTrainDatas[i].Month - lastMonth;
                    for(int j = 1;j <= missingMonths; j++)
                    {
                        if(trainDataCache.ContainsKey((int)lastMonth + 1))
                        {
                            resolvedData.Add(new ProductMonthlyTrainData()
                            {
                                Month = j + lastMonth,
                                CurrentMonthQuantity = trainDataCache[(int)lastMonth + j].CurrentMonthQuantity,
                                Next = 0
                            });
                        } 
                        else
                        {
                            resolvedData.Add(new ProductMonthlyTrainData()
                            {
                                Month = lastMonth + j,
                                CurrentMonthQuantity = resolvedData[resolvedData.Count - 1].CurrentMonthQuantity,
                                Next = 0
                            });
                        }
                    }
                    resolvedData.Add(productMonthlyTrainDatas[i]);
                } 
                else
                {
                    for(int j = (int)lastMonth + 1;j <= 12;j++)
                    {
                        if (trainDataCache.ContainsKey(j))
                        {
                            resolvedData.Add(new ProductMonthlyTrainData()
                            {
                                Month = j,
                                CurrentMonthQuantity = trainDataCache[j].CurrentMonthQuantity,
                                Next = 0
                            });
                        } 
                        else
                        {
                            resolvedData.Add(new ProductMonthlyTrainData()
                            {
                                Month = j,
                                CurrentMonthQuantity = resolvedData[resolvedData.Count - 1].CurrentMonthQuantity,
                                Next = 0
                            });
                        }
                    }
                    for(int j = 1;j < productMonthlyTrainDatas[i].Month;j++)
                    {
                        if (trainDataCache.ContainsKey(j))
                        {
                            resolvedData.Add(new ProductMonthlyTrainData()
                            {
                                Month = j,
                                CurrentMonthQuantity = trainDataCache[j].CurrentMonthQuantity,
                                Next = 0
                            });
                        }
                        else
                        {
                            resolvedData.Add(new ProductMonthlyTrainData()
                            {
                                Month = j,
                                CurrentMonthQuantity = resolvedData[resolvedData.Count - 1].CurrentMonthQuantity,
                                Next = 0
                            });
                        }
                    }
                    resolvedData.Add(productMonthlyTrainDatas[i]); 
                }
            }
            return resolvedData;
        }
        private async Task<List<ProductWeeklyTrainData>> PrepareIndividualProductWeeklyTrainingData(Product product, bool DiscountAppliedFrequently)
        {
            var query = from oi in _orderItemRepository.Table
                        join o in _orderRepository.Table on oi.OrderId equals o.Id
                        where oi.ProductId == product.Id
                        group oi by new
                        {
                            Year = o.CreatedOnUtc.Year,
                            Month = o.CreatedOnUtc.Month,
                            Day = o.CreatedOnUtc.Day,
                        } into goi
                        select new ProductDailyGroupedTrainData()
                        {
                            Year = goi.Key.Year, 
                            Month = goi.Key.Month,
                            Day = goi.Key.Day,
                            CurrentWeekQuantity = goi.Sum(x => x.Quantity),
                        };

            var trainData = await query.ToListAsync();

            var weekNoToWeeklyTrainDataMap = new Dictionary<Tuple<int,int,int>, TempProductWeeklyTrainData>();

            foreach (var row in trainData)
            {
                DateTime date = new DateTime((int)row.Year, (int)row.Month, (int)row.Day);
                int weekNoOfMonth = (date.Day - 1) / 7 + 1;

                if (weekNoToWeeklyTrainDataMap.ContainsKey(new Tuple<int,int,int>((int)row.Year, (int)row.Month, weekNoOfMonth)))
                {
                    weekNoToWeeklyTrainDataMap[new Tuple<int, int, int>((int)row.Year, (int)row.Month, weekNoOfMonth)].CurrentWeekQuantity += row.CurrentWeekQuantity;
                } else
                {
                    weekNoToWeeklyTrainDataMap[new Tuple<int, int, int>((int)row.Year, (int)row.Month, weekNoOfMonth)] = new TempProductWeeklyTrainData()
                    {
                        Year = row.Year,
                        Month = row.Month,
                        Week = weekNoOfMonth,
                        CurrentWeekQuantity = row.CurrentWeekQuantity,
                    };
                }
            }

            var weeklyGroupedTrainData = weekNoToWeeklyTrainDataMap.Values.ToList();

            var missingValuesHandledWeeklyTrainData = HandleMissingValueForIndividualProductWeeklyTrainData(weeklyGroupedTrainData);

            var completeTrainingData = missingValuesHandledWeeklyTrainData.Select((x, index) => new ProductWeeklyTrainData
            {
                Month = x.Month,
                Week = x.Week,
                CurrentWeekQuantity = x.CurrentWeekQuantity,
                Next = index == missingValuesHandledWeeklyTrainData.Count - 1 ? 0 : missingValuesHandledWeeklyTrainData[index + 1].CurrentWeekQuantity,
            }).ToList();

            return completeTrainingData;
        }

        private async Task<List<ProductMonthlyTrainData>> PrepareIndividualProductMonthlyTrainingData(Product product, bool DiscountAppliedFrequently)
        {
            var query = from oi in _orderItemRepository.Table
                        join o in _orderRepository.Table on oi.OrderId equals o.Id
                        where oi.ProductId == product.Id
                        group oi by new
                        {
                            Year = o.CreatedOnUtc.Year,
                            Month = o.CreatedOnUtc.Month,
                        } into goi
                        orderby new {goi.Key.Year, goi.Key.Month}
                        select new ProductMonthlyTrainData()
                        {
                            Month = goi.Key.Month,
                            CurrentMonthQuantity = goi.Sum(x => x.Quantity),
                        };

            var trainData = await query.ToListAsync();
            
            var missingValuesHandledMonthlyTrainData = HandleMissingValueForIndividualProductMonthlyTrainData(trainData);

            var completeTrainingData = missingValuesHandledMonthlyTrainData.Select((x, index) => new ProductMonthlyTrainData
            {
                Month = x.Month,
                CurrentMonthQuantity = x.CurrentMonthQuantity,
                Next = index == missingValuesHandledMonthlyTrainData.Count - 1 ? 0 : missingValuesHandledMonthlyTrainData[index + 1].CurrentMonthQuantity,
            }).ToList();

            return completeTrainingData;
        }

        #endregion

        #region Train and Save

        public async Task<(bool, string)> TrainIndividualProductWeeklySalesPredictionModelAsync(Product product, bool DiscountAppliedFrequently, bool logInfo = false)
        {
            var sucess = false;

            var productSalesHistory = await PrepareIndividualProductWeeklyTrainingData(product, DiscountAppliedFrequently);

            var mlMonthlyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelPathIndividualProductForcasting);
            var outputPathToSaveModel = _fileProvider.Combine(mlMonthlyModelPath, product.Id.ToString()+".zip");

            // save the training data to a csv file 
            CsvWriter.WriteToCsv(outputPathToSaveModel + "trainingData.csv", productSalesHistory);

            // train and save to the database
            ForecastingModelHelper.TrainAndSaveIndividualProductModel(_mlContext, productSalesHistory, outputPathToSaveModel);
            
            sucess = true;

            return (sucess, string.Empty);
        }

        public async Task<(bool, string)> TrainIndividualProductMonthlySalesPredictionModelAsync(Product product, bool DiscountAppliedFrequently, bool logInfo = false)
        {
            var sucess = false;

            var productSalesHistory = await PrepareIndividualProductMonthlyTrainingData(product, DiscountAppliedFrequently);

            var mlMonthlyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelPathIndividualProductForcasting);
            var outputPathToSaveModel = _fileProvider.Combine(mlMonthlyModelPath, product.Id.ToString() + "monthly" + ".zip");

            // save the training data to a csv file 
            CsvWriter.WriteToCsv(outputPathToSaveModel + "trainingData.csv", productSalesHistory);

            // train and save to the database
            ForecastingModelHelper.TrainAndSaveIndividualProductMonthlyModel(_mlContext, productSalesHistory, outputPathToSaveModel);
            
            sucess = true;

            return (sucess, string.Empty);
        }

        #endregion

        #region Forcast 

        public async Task<float> PredictSaleForEachIndividualProduct(Product product, bool DiscountAppliedFrequently)
        {
            var mlMonthlyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelPathIndividualProductForcasting);
            var outputPathToSaveModel = _fileProvider.Combine(mlMonthlyModelPath, product.Id.ToString() + ".zip");

            // get this week data 
            var sampleData = await PrepareIndividualProductWeeklyTrainingData(product, DiscountAppliedFrequently);
            var sampleDatum = sampleData[sampleData.Count - 1];

            var score = ForecastingModelHelper.PredictIndividualProductModel(_mlContext, outputPathToSaveModel, sampleDatum);

            return score;
        }

        public async Task<float> PredictMonthlySaleForEachIndividualProduct(Product product, bool DiscountAppliedFrequently)
        {
            var mlMonthlyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelPathIndividualProductForcasting);
            var outputPathToSaveModel = _fileProvider.Combine(mlMonthlyModelPath, product.Id.ToString() + "monthly" + ".zip");

            // get this week data 
            var sampleData = await PrepareIndividualProductMonthlyTrainingData(product, DiscountAppliedFrequently);
            var sampleDatum = sampleData[sampleData.Count - 1];

            var score = ForecastingModelHelper.PredictIndividualProductMonthlyModel(_mlContext, outputPathToSaveModel, sampleDatum);

            return score;
        }

        

        #endregion

        #endregion

    }
}
