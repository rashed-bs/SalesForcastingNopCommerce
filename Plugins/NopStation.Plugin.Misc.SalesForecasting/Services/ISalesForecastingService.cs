using Nop.Core.Domain.Customers;
using NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models;
using NopStation.Plugin.Misc.SalesForecasting.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NopStation.Plugin.Misc.SalesForecasting.Services
{
    public interface ISalesForecastingService
    {
        Task<IEnumerable<SalesData>> GetSalesHistoryDataAsync(int? productId = null);

        Task<IEnumerable<ProductData>> GetProductHistoryDataAsync(int? productId = null);

        Task<(bool, string)> TrainAndTestModelAsync(bool logInfo = false);

        Task<ProductUnitRegressionPrediction> ProductForecastingPredictionAsync(int productId);
    }
}