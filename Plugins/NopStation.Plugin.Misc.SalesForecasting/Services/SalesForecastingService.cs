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

namespace NopStation.Plugin.Misc.SalesForecasting.Services
{
    
    public partial class SalesForecastingService : ISalesForecastingService
    {
        #region ctor
        private static CacheKey CategoryGroupsCacheKey => new("CategoryGroupsCacheKey"); // todo: move it to the default
        private readonly IRepository<Order> _orderRepository;
            private readonly IRepository<ProductCategory> _productCategoryRepository;
            private readonly IRepository<OrderItem> _orderItemRepository;
            private readonly IRepository<Product> _productRepository;
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
                _storeContext = storeContext;
                _productService = productService;
                _discountService = discountService;
                _inventoryPredictionSettings = inventoryPredictionSettings;
                _logger = logger;
            }

        #endregion
        #region Utilities Prvious Version

        private IQueryable<SalesData> SalesHistoryQuery()
        {
            var query = from p1 in (from oi in (from oi in _orderItemRepository.Table
                                                join o in _orderRepository.Table on oi.OrderId equals o.Id
                                                group oi by new { OrderDate = o.CreatedOnUtc.Date } into goi
                                                select new { goi.Key.OrderDate, Units = goi.Sum(s => s.Quantity) })
                                    group oi by new { oi.OrderDate.Year, oi.OrderDate.Month } into goi
                                    select new
                                    {
                                        goi.Key.Year,
                                        goi.Key.Month,
                                        OrderDate = goi.Min(m => m.OrderDate.Date),
                                        Units = goi.Sum(s => s.Units),
                                        Avg = goi.Average(a => a.Units),
                                        Count = goi.CountExt(c => c.Units),
                                        Max = goi.Max(m => m.Units),
                                        Min = goi.Min(m => m.Units)
                                    })
                        select new SalesData
                        {
                            Year = p1.Year,
                            Month = p1.Month,
                            Units = p1.Units,
                            Avg = (float)p1.Avg,
                            Count = p1.Count,
                            Max = p1.Max,
                            Min = p1.Min,
                            Prev = Sql.Ext.Lag(p1.Units, Sql.Nulls.Respect, 1, 0).Over().OrderBy(p1.OrderDate.Date).ToValue(),
                            Next = Sql.Ext.Lead(p1.Units, Sql.Nulls.Respect, 1, 0).Over().OrderBy(p1.OrderDate.Date).ToValue(),
                        };

            return query;
        }

        private IQueryable<ProductData> ProductHistoryQuery(int? productId = null)
        {
            var query = from p1 in (from oi in (from oi in _orderItemRepository.Table
                                                join o in _orderRepository.Table on oi.OrderId equals o.Id
                                                group oi by new { OrderDate = o.CreatedOnUtc.Date, oi.ProductId } into goi
                                                select new { goi.Key.OrderDate, goi.Key.ProductId, Units = goi.Sum(s => s.Quantity) })
                                    group oi by new { oi.ProductId, oi.OrderDate.Year, oi.OrderDate.Month } into goi
                                    select new
                                    {
                                        goi.Key.ProductId,
                                        goi.Key.Year,
                                        goi.Key.Month,
                                        OrderDate = goi.Min(m => m.OrderDate.Date),
                                        Units = goi.Sum(s => s.Units),
                                        Avg = goi.Average(a => a.Units),
                                        Count = goi.CountExt(c => c.Units),
                                        Max = goi.Max(m => m.Units),
                                        Min = goi.Min(m => m.Units)
                                    })
                        join p2 in (from p2 in from oi in (from oi in _orderItemRepository.Table
                                                           join o in _orderRepository.Table on oi.OrderId equals o.Id
                                                           where (!productId.HasValue || oi.ProductId == productId.Value)
                                                           group oi by new { OrderDate = o.CreatedOnUtc.Date, oi.ProductId } into goi
                                                           select new { goi.Key.OrderDate, goi.Key.ProductId, Units = goi.Sum(s => s.Quantity) })
                                               group oi by new { oi.ProductId, oi.OrderDate.Year, oi.OrderDate.Month } into goi
                                               select new
                                               {
                                                   goi.Key.ProductId,
                                                   goi.Key.Year,
                                                   goi.Key.Month,
                                               }
                                    group p2 by p2.ProductId into goi
                                    where (goi.Count() >= 1)
                                    select new { ProductId = goi.Key })
                        on p1.ProductId equals p2.ProductId
                        select new ProductData
                        {
                            ProductId = p1.ProductId,
                            Year = p1.Year,
                            Month = p1.Month,
                            Units = p1.Units,
                            Avg = (float)p1.Avg,
                            Count = p1.Count,
                            Max = p1.Max,
                            Min = p1.Min,
                            Prev = Sql.Ext.Lag(p1.Units, Sql.Nulls.Respect, 1, 0).Over().PartitionBy(p1.ProductId).OrderBy(p1.ProductId).ThenBy(p1.OrderDate.Date).ToValue(),
                            Next = Sql.Ext.Lead(p1.Units, Sql.Nulls.Respect, 1, 0).Over().PartitionBy(p1.ProductId).OrderBy(p1.ProductId).ThenBy(p1.OrderDate.Date).ToValue(),
                        };
            return query;
        }

        #endregion
        #region Methods Previous Version

        public virtual async Task<IEnumerable<SalesData>> GetSalesHistoryDataAsync(int? productId = null)
        {
            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(SalesForecastingDefaults.SalesForecastingSalesDataKey, productId ?? 0);

            var salesHistory = await _staticCacheManager.GetAsync(cacheKey, async () =>
            {
                var query = SalesHistoryQuery();
                //return await query.Where(s => s.Next != 0 && s.Prev != 0).ToListAsync();
                return await query.ToListAsync();
            });

            return salesHistory;
        }

        public virtual async Task<IEnumerable<ProductData>> GetProductHistoryDataAsync(int? productId = null)
        {
            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(SalesForecastingDefaults.SalesForecastingProductDataKey, productId ?? 0);

            var productHistory = await _staticCacheManager.GetAsync(cacheKey, async () =>
            {
                var query = ProductHistoryQuery(productId);
                //return await query.Where(p => p.Next != 0 && p.Prev != 0).ToListAsync();
                return await query.ToListAsync();
            });

            return productHistory;
        }

        public virtual async Task<(bool, string)> TrainAndTestModelAsyncOLD(bool logInfo = false)
        {
            //var res = await SalesHistoryQueryNew().ToListAsync();

            var sucess = false;

            //var salesHistory = await GetSalesHistoryDataAsync();
            var query = from oi in _orderItemRepository.Table
                        select new SalesData
                        {
                            Avg = 1,
                            Count = 12,
                            Max = 10,
                            Min = 1,
                            Month = 10,
                            Next = 14,
                            Prev = 23,
                            Units = 23,
                            Year = 2023
                        };
            var salesHistory = await query.ToListAsync();

            // Get product history for the selected product
            //var productHistory = await GetProductHistoryDataAsync();

            var mLModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelPath);

            if (!_fileProvider.DirectoryExists(mLModelPath))
                _fileProvider.CreateDirectory(mLModelPath);

            var regressionSalesForecastingDirectory = _fileProvider.Combine(mLModelPath, SalesForecastingDefaults.RegressionSalesForecastingModelPath);

            if (_fileProvider.FileExists(regressionSalesForecastingDirectory))
                _fileProvider.DeleteFile(regressionSalesForecastingDirectory);

            ForecastingModelHelper.TrainAndSaveRegressionSalesForecastingModel(_mlContext, salesHistory, regressionSalesForecastingDirectory);

            var testSalesData = salesHistory.LastOrDefault();

            ForecastingModelHelper.RegressionSalesForecastingPrediction(_mlContext, testSalesData, regressionSalesForecastingDirectory);

            /*var regressionProductForecastingDirectory = _fileProvider.Combine(mLModelPath, SalesForecastingDefaults.RegressionProductForecastingModelPath);

            if (_fileProvider.FileExists(regressionProductForecastingDirectory))
                _fileProvider.DeleteFile(regressionProductForecastingDirectory);

            //ForecastingModelHelper.TrainAndSaveRegressionProductForecastingModel(_mlContext, productHistory, regressionProductForecastingDirectory);

            var productHistoryOrderBySales = productHistory.GroupBy(p => p.ProductId)
                .Select(p => new { ProductId = p.Key, Sales = p.Count() })
                .OrderBy(o => o.Sales);

            var testProductData = (await GetProductHistoryDataAsync((int)productHistoryOrderBySales.LastOrDefault().ProductId)).LastOrDefault();

            ForecastingModelHelper.RegressionProductForecastingPrediction(_mlContext, testProductData, regressionProductForecastingDirectory);*/

            sucess = true;

            return (sucess, string.Empty);
        }

        public virtual async Task<ProductUnitRegressionPrediction> ProductForecastingPredictionAsync(int productId)
        {
            // Get product history for the selected product
            var productHistory = await GetProductHistoryDataAsync(productId);

            var mLModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelPath);

            var regressionProductForecastingDirectory = _fileProvider.Combine(mLModelPath, SalesForecastingDefaults.RegressionProductForecastingModelPath);

            var productData = productHistory.LastOrDefault();

            var nextMonthUnitDemand = ForecastingModelHelper.RegressionProductForecastingPrediction(_mlContext, productData, regressionProductForecastingDirectory);

            return (nextMonthUnitDemand);
        }

        #endregion



        #region Utilities New Version

        // Date creation method
        private static DateTime GetDateTime(_SalesData salesData)
        {
            int year = (int)salesData.Year;
            int month = (int)salesData.Month;
            int day = (int)salesData.Day;
            return new DateTime(year, month, day);
        }

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
        private static double[] CalculateSlopesNextValue(List<_SalesData> dataset)
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
        private static double[] CalculateSlopesPrevValue(List<_SalesData> dataset)
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
        public static List<_SalesData> FillMissingValues(List<_SalesData> dataset, List<int> weekends)
        {
            var filledDataset = new List<_SalesData>();

            double[] slopesNextValue = CalculateSlopesNextValue(dataset);
            double[] slopesPrevValue = CalculateSlopesPrevValue(dataset);

            for (int i = 0; i < dataset.Count; i++)
            {
                filledDataset.Add(dataset[i]);

                // Check if there's a missing value between two consecutive data points
                if (i < dataset.Count - 1 && GetDateTime(dataset[i + 1]).Subtract(GetDateTime(dataset[i])).Days > 1)
                {
                    DateTime startDate = GetDateTime(dataset[i]);
                    DateTime endDate = GetDateTime(dataset[i+1]);

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
                        filledDataset.Add(new _SalesData
                        {
                            Year = missingDate.Year,
                            Month = missingDate.Month,
                            Day = missingDate.Day,
                            CategoryID = dataset[i].CategoryID,
                            IsWeekend = weekends.Contains((int)missingDate.DayOfWeek) ? (float)1.0 : (float)0.0,
                            ShippingCharge = dataset[i].ShippingCharge,
                            PercentageDiscount = dataset[i].PercentageDiscount,
                            PreviousDayUnits =float.Max((float)interpolatedPrevValue, 0),
                            NextDayUnits = float.Max((float)interpolatedNextValue, 0),
                        });
                    }
                }
            }

            return filledDataset;
        }

        // add respective discounts 
        public async Task<List<_SalesData>> AddDiscountData(List<_SalesData> dataset)
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

        private async Task<List<_SalesData>> SalesHistoryQueryWeekly(List<int> categories)
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
                        select new _SalesData
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
                //return FillMissingValues(salesData, weekends);
                return salesData;
            else
                return salesData;
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

        private string CategoryGroupModelName(List<CategoryAvgPrice> categoryAvgPrices)
        {
            string categoryGroupName = "TimeSeriesSSa_Categories";
            foreach (var categoryAvgPrice in categoryAvgPrices)
            {
                categoryGroupName += "_" + categoryAvgPrice.CategoryId.ToString();
            }
            return categoryGroupName;
        }

        #endregion


        #region Methods New Version
        public virtual async Task<(bool, string)> TrainAndTestModelAsync(bool logInfo = false)
        {
            var sucess = false;

            // get the category groups 
            var categoryGroups = await GetCategoryGroups();
            
            // save the group in cache
            await _staticCacheManager.SetAsync(_staticCacheManager.PrepareKeyForDefaultCache(CategoryGroupsCacheKey), categoryGroups);

            // clean up the directories 
            var mLModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelPath);
            if (_fileProvider.DirectoryExists(mLModelPath))
                _fileProvider.DeleteDirectory(mLModelPath);
            _fileProvider.CreateDirectory(mLModelPath);

            var mlMonthlyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelMonthlyPredictionPath);
            if (_fileProvider.DirectoryExists(mlMonthlyModelPath))
                _fileProvider.DeleteDirectory(mlMonthlyModelPath);
            _fileProvider.CreateDirectory(mlMonthlyModelPath);

            var mlWeeklyModelPath = _fileProvider.MapPath(SalesForecastingDefaults.MLModelWeeklyPredictionPath);
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
                var salesTrainHistory = await SalesHistoryQueryWeekly(categories);
                var modelName = CategoryGroupModelName(categoryGroup);
                var outputPathToSaveModel = _fileProvider.Combine(mlWeeklyModelPath, modelName);
                if(salesTrainHistory.Count() > 14)
                {
                    // add the discount data to it 
                    salesTrainHistory = await AddDiscountData(salesTrainHistory);
                    ForecastingModelHelper.TrainAndSaveTimeSeriesSalesForecastingModelWeekly(_mlContext, salesTrainHistory, outputPathToSaveModel);
                }
            }

            /*var salesHistory = await GetSalesHistoryDataAsync();
            var query = from oi in _orderItemRepository.Table
                        select new SalesData
                        {
                            Avg = 1,
                            Count = 12,
                            Max = 10,
                            Min = 1,
                            Month = 10,
                            Next = 14,
                            Prev = 23,
                            Units = 23,
                            Year = 2023
                        };
            var salesHistory = await query.ToListAsync();

            Get product history for the selected product
            var productHistory = await GetProductHistoryDataAsync();

            var regressionSalesForecastingDirectory = _fileProvider.Combine(mLModelPath, SalesForecastingDefaults.RegressionSalesForecastingModelPath);

            if (_fileProvider.FileExists(regressionSalesForecastingDirectory))
                _fileProvider.DeleteFile(regressionSalesForecastingDirectory);

            ForecastingModelHelper.TrainAndSaveRegressionSalesForecastingModel(_mlContext, salesHistory, regressionSalesForecastingDirectory);

            var testSalesData = salesHistory.LastOrDefault();

            ForecastingModelHelper.RegressionSalesForecastingPrediction(_mlContext, testSalesData, regressionSalesForecastingDirectory);

            var regressionProductForecastingDirectory = _fileProvider.Combine(mLModelPath, SalesForecastingDefaults.RegressionProductForecastingModelPath);

            if (_fileProvider.FileExists(regressionProductForecastingDirectory))
                _fileProvider.DeleteFile(regressionProductForecastingDirectory);

            //ForecastingModelHelper.TrainAndSaveRegressionProductForecastingModel(_mlContext, productHistory, regressionProductForecastingDirectory);

            var productHistoryOrderBySales = productHistory.GroupBy(p => p.ProductId)
                .Select(p => new { ProductId = p.Key, Sales = p.Count() })
                .OrderBy(o => o.Sales);

            var testProductData = (await GetProductHistoryDataAsync((int)productHistoryOrderBySales.LastOrDefault().ProductId)).LastOrDefault();

            ForecastingModelHelper.RegressionProductForecastingPrediction(_mlContext, testProductData, regressionProductForecastingDirectory);*/

            sucess = true;

            return (sucess, string.Empty);
        }
        #endregion

    }
}
