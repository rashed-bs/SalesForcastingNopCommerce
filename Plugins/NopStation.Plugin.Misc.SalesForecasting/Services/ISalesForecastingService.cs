﻿using Microsoft.ML;
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
        Task<(bool, string)> TrainWeeklySalesPredictionTimeSeriesModelAsync(bool logInfo = false);

        Task<(bool, string)> TrainLargeFeatureProductSalesPredictionModelAsync(bool logInfo = false);

        Task<(bool, string)> TrainBaseCategoryWiseProductSalesPredictionModelAsync(bool logInfo = false);

        Task<(bool, string)> TrainBaseLocationWiseProductSalesPredictionModelAsync(bool logInfo = false);

        Task<(bool, string)> TrainBaseCategoryAvgPriceWiseProductSalesPredictionModelAsync(bool logInfo = false);

        Task<(bool, string)> TrainBaseMonthWiseProductSalesPredictionModelAsync(bool logInfo = false);

        Task<List<SalesData>> DailySalesHistoryQueryLastMonth();

        List<float> TimeSeriesPredictWeeklySales(bool logInfo = false);

        Task<List<MonthBaseModelInputData>> MonthlySalesHistoryQueryLastYear();
        Task<List<MonthlySalesCategoryContribution>> PredictCategorySalesContribution();

        Task<float> PredictEnsembleNextMonthSales();

        public void PathPreparation();
    }
}