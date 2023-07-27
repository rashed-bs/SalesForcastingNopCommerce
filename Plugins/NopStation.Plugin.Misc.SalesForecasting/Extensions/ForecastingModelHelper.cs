using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using NopStation.Plugin.Misc.SalesForecasting.Models;

namespace NopStation.Plugin.Misc.SalesForecasting.Extensions
{
    public class ForecastingModelHelper
    {
        #region Regression Model Helper

        /// <summary>
        /// Build model for predicting next month's product unit sales using regression forecasting.
        /// </summary>
        /// <param name="mlContext">ML.NET context.</param>
        /// <param name="productDataSeries">ML.NET IDataView representing the loaded product data series.</param>
        /// <param name="outputModelPath">Model path.</param>
        public static void TrainAndSaveRegressionSalesForecastingModel(MLContext mlContext, IEnumerable<SalesData> salesHistory, string outputModelPath)
        {
            var trainingDataView = mlContext.Data.LoadFromEnumerable(salesHistory);

            var trainer = mlContext.Regression.Trainers.FastTreeTweedie(labelColumnName: "Label", featureColumnName: "Features");

            var trainingPipeline = mlContext.Transforms.Concatenate(outputColumnName: "NumFeatures", nameof(SalesData.Year), nameof(SalesData.Month), nameof(SalesData.Units), nameof(SalesData.Avg), nameof(SalesData.Count),
                nameof(SalesData.Max), nameof(SalesData.Min), nameof(SalesData.Prev))
                .Append(mlContext.Transforms.Concatenate(outputColumnName: "Features", "NumFeatures"))
                .Append(mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(SalesData.Next)))
                .Append(trainer);

            // Cross-Validate with single dataset (since we don't have two datasets, one for training and for evaluate)
            // in order to evaluate and get the model's accuracy metrics

            var crossValidationResults = mlContext.Regression.CrossValidate(data: trainingDataView, estimator: trainingPipeline, numberOfFolds: 6, labelColumnName: "Label");

            // Train the model.
            var model = trainingPipeline.Fit(trainingDataView);

            // Save the model for later comsumption from end-user apps.
            mlContext.Model.Save(model, trainingDataView.Schema, outputModelPath);
        }

        /// <summary>
        /// Predict samples using saved model.
        /// </summary>
        /// <param name="mlContext">ML.NET context.</param>
        /// <param name="lastMonthProductData">The last month of sales in the monthly data series.</param>
        /// <param name="outputModelPath">Model file path</param>
        public static SalesRegressionPrediction RegressionSalesForecastingPrediction(MLContext mlContext, SalesData salesData, string outputModelPath)
        {
            // Read the model that has been previously saved by the method SaveModel.
            ITransformer trainedModel;
            using (var stream = File.OpenRead(outputModelPath))
            {
                trainedModel = mlContext.Model.Load(stream, out var modelInputSchema);
            }

            var predictionEngine = mlContext.Model.CreatePredictionEngine<SalesData, SalesRegressionPrediction>(trainedModel);

            // Predict the nextperiod/month forecast to the one provided
            var regressionPrediction = predictionEngine.Predict(salesData);

            return regressionPrediction;
        }

        /// <summary>
        /// Build model for predicting next month's product unit sales using regression forecasting.
        /// </summary>
        /// <param name="mlContext">ML.NET context.</param>
        /// <param name="productDataSeries">ML.NET IDataView representing the loaded product data series.</param>
        /// <param name="outputModelPath">Model path.</param>
        public static void TrainAndSaveRegressionProductForecastingModel(MLContext mlContext, IEnumerable<ProductData> productHistory, string outputModelPath)
        {
            var trainingDataView = mlContext.Data.LoadFromEnumerable(productHistory);

            var trainer = mlContext.Regression.Trainers.FastTreeTweedie(labelColumnName: "Label", featureColumnName: "Features");

            var trainingPipeline = mlContext.Transforms.Concatenate(outputColumnName: "NumFeatures", nameof(ProductData.Year), nameof(ProductData.Month), nameof(ProductData.Units), nameof(ProductData.Avg), nameof(ProductData.Count),
                nameof(ProductData.Max), nameof(ProductData.Min), nameof(ProductData.Prev))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "CatFeatures", inputColumnName: nameof(ProductData.ProductId)))
                .Append(mlContext.Transforms.Concatenate(outputColumnName: "Features", "NumFeatures", "CatFeatures"))
                .Append(mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(ProductData.Next)))
                .Append(trainer);

            // Cross-Validate with single dataset (since we don't have two datasets, one for training and for evaluate)
            // in order to evaluate and get the model's accuracy metrics

            var crossValidationResults = mlContext.Regression.CrossValidate(data: trainingDataView, estimator: trainingPipeline, numberOfFolds: 6, labelColumnName: "Label");

            // Train the model.
            var model = trainingPipeline.Fit(trainingDataView);

            // Save the model for later comsumption from end-user apps.
            mlContext.Model.Save(model, trainingDataView.Schema, outputModelPath);
        }

        /// <summary>
        /// Predict samples using saved model.
        /// </summary>
        /// <param name="mlContext">ML.NET context.</param>
        /// <param name="lastMonthProductData">The last month of product data in the monthly data series.</param>
        /// <param name="outputModelPath">Model file path</param>
        public static ProductUnitRegressionPrediction RegressionProductForecastingPrediction(MLContext mlContext, ProductData productData, string outputModelPath)
        {
            // Read the model that has been previously saved by the method SaveModel.
            ITransformer trainedModel;
            using (var stream = File.OpenRead(outputModelPath))
            {
                trainedModel = mlContext.Model.Load(stream, out var modelInputSchema);
            }

            var predictionEngine = mlContext.Model.CreatePredictionEngine<ProductData, ProductUnitRegressionPrediction>(trainedModel);

            // Predict the nextperiod/month forecast to the one provided
            var regressionPrediction = predictionEngine.Predict(productData);     
            
            return regressionPrediction;
        }

        #endregion

        #region TimeSeries Model Helper Weekly

        public static void TrainAndSaveTimeSeriesSalesForecastingModelWeekly(MLContext mlContext, IEnumerable<_SalesData> salesHistory, string outputModelPath)
        {
            // create the dataView
            var salesDataView = mlContext.Data.LoadFromEnumerable(salesHistory);

            // create the pipeline 
            IEstimator<ITransformer> weeklySalesEstimator = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: nameof(WeeklySalesTimeSeriesPrediction.ForecastedWeeklySalesUnits),
                inputColumnName: nameof(_SalesData.NextDayUnits), // This is the column being forecasted.
                windowSize: 7, // Window size is set to the time period represented in the product data cycle; our product cycle is based on 12 months, so this is set to a factor of 12, e.g. 3.
                seriesLength: salesHistory.Count(), // This parameter specifies the number of data points that are used when performing a forecast.
                trainSize: salesHistory.Count(), // This parameter specifies the total number of data points in the input time series, starting from the beginning.
                horizon: 7, // Indicates the number of values to forecast; 1 indicates that the next month of product units will be forecasted.
                confidenceLevel: 0.95f, // Indicates the likelihood the real observed value will fall within the specified interval bounds.
                confidenceLowerBoundColumn: nameof(WeeklySalesTimeSeriesPrediction.ConfidenceLowerBound), //This is the name of the column that will be used to store the lower interval bound for each forecasted value.
                confidenceUpperBoundColumn: nameof(WeeklySalesTimeSeriesPrediction.ConfidenceUpperBound)); //This is the name of the column that will be used to store the upper interval bound for each forecasted value.
            
            // Train the forecasting model for the specified product's data series.
            var forecastTransformer = weeklySalesEstimator.Fit(salesDataView);

            // Create the forecast engine used for creating predictions.
            var forecastEngine = forecastTransformer.CreateTimeSeriesEngine<_SalesData, WeeklySalesTimeSeriesPrediction>(mlContext);

            // Save the forecasting model so that it can be loaded within an end-user app.
            forecastEngine.CheckPoint(mlContext, outputModelPath);
        }

        public static WeeklySalesTimeSeriesPrediction TimeSeriesSalesForecastingPredictionWeekly(MLContext mlContext, string outputModelPath)
        {
            // Load the forecast engine that has been previously saved.
            ITransformer forecaster;
            using (var file = File.OpenRead(outputModelPath))
            {
                forecaster = mlContext.Model.Load(file, out var schema);
            }

            // We must create a new prediction engine from the persisted model.
            var forecastEngine = forecaster.CreateTimeSeriesEngine<_SalesData, WeeklySalesTimeSeriesPrediction>(mlContext);

            // Get the prediction; this will include the forecasted product units sold for the next 2 months since this the time period specified in the `horizon` parameter when the forecast estimator was originally created.
            var salesPrediction = forecastEngine.Predict();

            return salesPrediction;
        }

        #endregion
    }
}
