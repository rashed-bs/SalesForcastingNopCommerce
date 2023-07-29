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
        #region TimeSeries Model Helper Weekly

        public static void TrainAndSaveTimeSeriesSalesForecastingModelWeekly(MLContext mlContext, IEnumerable<SalesData> salesHistory, string outputModelPath)
        {
            var salesDataView = mlContext.Data.LoadFromEnumerable(salesHistory);

            IEstimator<ITransformer> weeklySalesEstimator = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: nameof(WeeklySalesTimeSeriesPrediction.ForecastedWeeklySalesUnits),
                inputColumnName: nameof(SalesData.NextDayUnits), // This is the column being forecasted.
                windowSize: 7, 
                seriesLength: salesHistory.Count(), 
                trainSize: salesHistory.Count(), 
                horizon: 7, 
                confidenceLevel: 0.95f, 
                confidenceLowerBoundColumn: nameof(WeeklySalesTimeSeriesPrediction.ConfidenceLowerBound),
                confidenceUpperBoundColumn: nameof(WeeklySalesTimeSeriesPrediction.ConfidenceUpperBound)); 
            
            var forecastTransformer = weeklySalesEstimator.Fit(salesDataView);

            var forecastEngine = forecastTransformer.CreateTimeSeriesEngine<SalesData, WeeklySalesTimeSeriesPrediction>(mlContext);

            forecastEngine.CheckPoint(mlContext, outputModelPath);

            //var pred = TimeSeriesSalesForecastingPredictionWeekly(mlContext, outputModelPath);
        }

        public static WeeklySalesTimeSeriesPrediction TimeSeriesSalesForecastingPredictionWeekly(MLContext mlContext, string outputModelPath)
        {
            // Load the forecast engine that has been previously saved.
            ITransformer forecaster;
            using (var file = File.OpenRead(outputModelPath))
            {
                forecaster = mlContext.Model.Load(file, out var schema);
            }

            var forecastEngine = forecaster.CreateTimeSeriesEngine<SalesData, WeeklySalesTimeSeriesPrediction>(mlContext);

            var salesPrediction = forecastEngine.Predict();

            return salesPrediction;
        }

        #endregion

        #region Regression Model Helper (Large Feature space)

        public static void TrainAndSaveLargeFeatureRegressionProductSalesForecastingModel(MLContext mlContext, List<ProductData> productSalesHistory, string outputModelPath)
        {
            var trainingDataView = mlContext.Data.LoadFromEnumerable(productSalesHistory);

            var trainer = mlContext.Regression.Trainers.FastTreeTweedie(labelColumnName: "Label", featureColumnName: "Features");

            var trainingPipeline = mlContext.Transforms.Concatenate(outputColumnName: "NumFeatures", nameof(ProductData.Month), nameof(ProductData.Day), nameof(ProductData.DiscountRate), nameof(ProductData.CategoryAvgPrice))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "CategoryFeatures", inputColumnName: nameof(ProductData.CategoryId)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "CountryFeatures", inputColumnName: nameof(ProductData.CountryId)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "WeekendFeatures", inputColumnName: nameof(ProductData.IsWeekend)))
                .Append(mlContext.Transforms.Concatenate(outputColumnName: "Features", "NumFeatures", "CategoryFeatures", "CountryFeatures", "WeekendFeatures"))
                .Append(mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(ProductData.Units)))
                .Append(trainer);

            var crossValidationResults = mlContext.Regression.CrossValidate(data: trainingDataView, estimator: trainingPipeline, numberOfFolds: 6, labelColumnName: "Label");

            // Train the model.
            var model = trainingPipeline.Fit(trainingDataView);

            // Save the model for later comsumption from end-user apps.
            mlContext.Model.Save(model, trainingDataView.Schema, outputModelPath);

            var pred = LargeFeatureRegressionProductSalesForecastingPrediction(mlContext, new ProductData
            {
               CategoryAvgPrice = 24,
               CategoryId = 2, 
               DiscountRate = 0,
               CountryId = 1,
               Day = 23, 
               IsWeekend = 1, 
               Month = 1, 
               Units = 2
            }, outputModelPath);
        }

        public static ProductUnitRegressionPrediction LargeFeatureRegressionProductSalesForecastingPrediction(MLContext mlContext, ProductData testProductData, string outputModelPath)
        {
            // Read the model that has been previously saved by the method SaveModel.
            ITransformer trainedModel;
            using (var stream = File.OpenRead(outputModelPath))
            {
                trainedModel = mlContext.Model.Load(stream, out var modelInputSchema);
            }

            var predictionEngine = mlContext.Model.CreatePredictionEngine<ProductData, ProductUnitRegressionPrediction>(trainedModel);

            // Predict the nextperiod/month forecast to the one provided
            var regressionPrediction = predictionEngine.Predict(testProductData);

            return regressionPrediction;
        }

        #endregion

        #region Base Category Wise Model
        public static void TrainAndSaveCategoryWiseBaseModel(MLContext mlContext, List<CategoryBaseModelData> productSalesHistory, string outputModelPath)
        {
            var trainingDataView = mlContext.Data.LoadFromEnumerable(productSalesHistory);

            var trainer = mlContext.Regression.Trainers.FastTreeTweedie(labelColumnName: "Label", featureColumnName: "Features");

            var trainingPipeline = mlContext.Transforms.Concatenate(outputColumnName: "NumFeatures", nameof(CategoryBaseModelData.UnitsSoldCurrent), nameof(CategoryBaseModelData.UnitsSoldPrev))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "CategoryFeatures", inputColumnName: nameof(CategoryBaseModelData.CategoryId)))
                .Append(mlContext.Transforms.Concatenate(outputColumnName: "Features", "NumFeatures", "CategoryFeatures"))
                .Append(mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(CategoryBaseModelData.Next)))
                .Append(trainer);

            var crossValidationResults = mlContext.Regression.CrossValidate(data: trainingDataView, estimator: trainingPipeline, numberOfFolds: 6, labelColumnName: "Label");

            // Train the model.
            var model = trainingPipeline.Fit(trainingDataView);

            // Save the model for later comsumption from end-user apps.
            mlContext.Model.Save(model, trainingDataView.Schema, outputModelPath);
        }

        #endregion

        #region Base Location Wise Model
        public static void TrainAndSaveLocationWiseBaseModel(MLContext mlContext, List<LocationBaseModelData> productSalesHistory, string outputModelPath)
        {
            var trainingDataView = mlContext.Data.LoadFromEnumerable(productSalesHistory);

            var trainer = mlContext.Regression.Trainers.FastTreeTweedie(labelColumnName: "Label", featureColumnName: "Features");

            var trainingPipeline = mlContext.Transforms.Concatenate(outputColumnName: "NumFeatures", nameof(LocationBaseModelData.UnitsSoldCurrent), nameof(LocationBaseModelData.UnitsSoldPrev))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "CountryFeatures", inputColumnName: nameof(LocationBaseModelData.CountryId)))
                .Append(mlContext.Transforms.Concatenate(outputColumnName: "Features", "NumFeatures", "CountryFeatures"))
                .Append(mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(LocationBaseModelData.Next)))
                .Append(trainer);

            var crossValidationResults = mlContext.Regression.CrossValidate(data: trainingDataView, estimator: trainingPipeline, numberOfFolds: 6, labelColumnName: "Label");

            // Train the model.
            var model = trainingPipeline.Fit(trainingDataView);

            // Save the model for later comsumption from end-user apps.
            mlContext.Model.Save(model, trainingDataView.Schema, outputModelPath);
        }
        #endregion

        #region Base Category Avg Price Wise Model
        public static void TrainAndSaveAveragePriceWiseBaseModel(MLContext mlContext, List<CategoryAvgPriceBaseModelData> productSalesHistory, string outputModelPath)
        {
            var trainingDataView = mlContext.Data.LoadFromEnumerable(productSalesHistory);

            var trainer = mlContext.Regression.Trainers.FastTreeTweedie(labelColumnName: "Label", featureColumnName: "Features");

            var trainingPipeline = mlContext.Transforms.Concatenate(outputColumnName: "NumFeatures", nameof(CategoryAvgPriceBaseModelData.UnitsSoldCurrent),, nameof(CategoryAvgPriceBaseModelData.CategoryAvgPrice))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "CategoryFeatures", inputColumnName: nameof(CategoryAvgPriceBaseModelData.CategoryId)))
                .Append(mlContext.Transforms.Concatenate(outputColumnName: "Features", "NumFeatures", "CategoryFeatures"))
                .Append(mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(CategoryAvgPriceBaseModelData.Next)))
                .Append(trainer);

            var crossValidationResults = mlContext.Regression.CrossValidate(data: trainingDataView, estimator: trainingPipeline, numberOfFolds: 6, labelColumnName: "Label");

            // Train the model.
            var model = trainingPipeline.Fit(trainingDataView);

            // Save the model for later comsumption from end-user apps.
            mlContext.Model.Save(model, trainingDataView.Schema, outputModelPath);
        }
        #endregion

        #region Base Month Wise Model
        #endregion

        #region Ensemble Model
        #endregion
    }
}
