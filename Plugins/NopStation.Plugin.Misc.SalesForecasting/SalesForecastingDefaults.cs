using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Caching;

namespace NopStation.Plugin.Misc.SalesForecasting
{
    public static class SalesForecastingDefaults
    {
        // <summary>
        /// Gets a plugin system name
        /// </summary>
        public static string SystemName => "NopStation.Plugin.Misc.SalesForecasting";

        public static string MLModelPathTimeSeries => "Plugins/NopStation.Plugin.Misc.SalesForecasting/MLModelFilesTimeSeries";

        public static string MLModelMonthlyPredictionPathTimeSeries => "Plugins/NopStation.Plugin.Misc.SalesForecasting/MLModelFilesTimeSeries/MonthlyModels";

        public static string MLModelWeeklyPredictionPathTimeSeries => "Plugins/NopStation.Plugin.Misc.SalesForecasting/MLModelFilesTimeSeries/WeeklyModels";

        public static string MLModelPathRegression => "Plugins/NopStation.Plugin.Misc.SalesForecasting/MLModelFilesRegression";

        public static string MLModelMonthlyPredictionPathRegression => "Plugins/NopStation.Plugin.Misc.SalesForecasting/MLModelFilesRegression/MonthlyModels";

        public static string MLModelWeeklyPredictionPathRegression => "Plugins/NopStation.Plugin.Misc.SalesForecasting/MLModelFilesRegression/WeeklyModels";

        public static string MLModelPathEnsemble => "Plugins/NopStation.Plugin.Misc.SalesForecasting/MLModelPathEnsemble";

        public static string MLModelMonthlyPredictionPathEnsemble => "Plugins/NopStation.Plugin.Misc.SalesForecasting/MLModelPathEnsemble/MonthlyModels";

        public static string MLModelWeeklyPredictionPathEnsemble => "Plugins/NopStation.Plugin.Misc.SalesForecasting/MLModelPathEnsemble/WeeklyModels";
        public static string ProductSalesCategoryWiseBaseModel => "Product_Category_Wise_Base_Model";

        public static string ProductSalesLocationWiseBaseModel => "Product_Location_Wise_Base_Model";
        public static string ProductSalesCategoryAvgPriceWiseBaseModel => "Product_Category_AvgPrice_Wise_Base_Model";
        public static string ProductSalesMonthWiseBaseModel => "Product_Month_Wise_Base_Model";
        public static string RegressionProductSalesFastTreeWeedieModelName => "Product_Sales_Regression_fastTreeTweedie";

        public static string RegressionProductForecastingModelPath => "product_month_fastTreeTweedie.zip";

        public static string TimeSeriesProductForecastingForecastingModelPath => "product{0}_month_timeSeriesSSA.zip";

        public static CacheKey SalesForecastingSalesDataKey => new("NopStation.Plugin.Misc.SalesForecasting.SalesData.{0}", SalesForecastingSalesDataPrefix);
        public static string SalesForecastingSalesDataPrefix => "NopStation.Plugin.Misc.SalesForecasting.SalesData.";

        public static CacheKey SalesForecastingProductDataKey => new("NopStation.Plugin.Misc.SalesForecasting.ProductData.{0}", SalesForecastingProductDataPrefix);
        public static CacheKey CategoryGroupsCacheKey => new("CategoryGroupsCacheKey"); 
        public static CacheKey CategorySubToRootMappingCacheKey => new("CategorySubToRootMappingCacheKey"); 
        public static CacheKey RootCategoryAvgPricesCacheKey => new("RootCategoryAvgPricesCacheKey"); 
        public static CacheKey TopSellingCategoriesCacheKey => new("RootCategoryAvgPricesCacheKey");
        public static CacheKey TopOrderingLocationsCacheKey => new("TopOrderingLocationsCacheKey");
        public static string SalesForecastingProductDataPrefix => "NopStation.Plugin.Misc.SalesForecasting.ProductData.";

    }
}
