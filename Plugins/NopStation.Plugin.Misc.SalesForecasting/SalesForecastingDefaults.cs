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

        public static string MLModelPath => "Plugins/NopStation.Plugin.Misc.SalesForecasting/MLModelFiles";

        public static string MLModelMonthlyPredictionPath => "Plugins/NopStation.Plugin.Misc.SalesForecasting/MLModelFiles/MonthlyModels";

        public static string MLModelWeeklyPredictionPath => "Plugins/NopStation.Plugin.Misc.SalesForecasting/MLModelFiles/WeeklyModels";

        public static string RegressionSalesForecastingModelPath => "sales_fastTreeTweedie.zip";

        public static string RegressionProductForecastingModelPath => "product_month_fastTreeTweedie.zip";

        public static string TimeSeriesProductForecastingForecastingModelPath => "product{0}_month_timeSeriesSSA.zip";

        public static CacheKey SalesForecastingSalesDataKey => new("NopStation.Plugin.Misc.SalesForecasting.SalesData.{0}", SalesForecastingSalesDataPrefix);
        public static string SalesForecastingSalesDataPrefix => "NopStation.Plugin.Misc.SalesForecasting.SalesData.";

        public static CacheKey SalesForecastingProductDataKey => new("NopStation.Plugin.Misc.SalesForecasting.ProductData.{0}", SalesForecastingProductDataPrefix);
        public static string SalesForecastingProductDataPrefix => "NopStation.Plugin.Misc.SalesForecasting.ProductData.";
    }
}
