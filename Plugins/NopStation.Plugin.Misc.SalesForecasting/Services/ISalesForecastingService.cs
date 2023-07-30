using Microsoft.ML;
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
        Task<(bool, string)> TrainWeeklySalesPredictionModelAsync(bool logInfo = false);

        Task<(bool, string)> TrainLargeFeatureProductSalesPredictionModelAsync(bool logInfo = false);

        Task<(bool, string)> TrainBaseCategoryWiseProductSalesPredictionModelAsync(bool logInfo = false);

        Task<(bool, string)> TrainBaseLocationWiseProductSalesPredictionModelAsync(bool logInfo = false);

        Task<(bool, string)> TrainBaseCategoryAvgPriceWiseProductSalesPredictionModelAsync(bool logInfo = false);

        Task<(bool, string)> TrainBaseMonthWiseProductSalesPredictionModelAsync(bool logInfo = false);

        Task<(bool, string)> TrainEnsembleMetaModelAsync(bool logInfo = false);

        public void PathPreparation();
    }
}