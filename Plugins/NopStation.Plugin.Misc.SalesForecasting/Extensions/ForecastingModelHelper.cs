using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;
using Microsoft.ML.Trainers.LightGbm;
using Microsoft.ML.Transforms.TimeSeries;
using NopStation.Plugin.Misc.SalesForecasting.Helpers;
using NopStation.Plugin.Misc.SalesForecasting.Models;
using NUglify.Helpers;

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

        #region Base model pipelines
        public static IEstimator<ITransformer> BuildPipelineForCategoryBaseModel(MLContext mlContext)
        {
            // Data process configuration with pipeline data transformations
            var pipeline = mlContext.Transforms.ReplaceMissingValues(new[] { new InputOutputColumnPair(@"CategoryId", @"CategoryId"), new InputOutputColumnPair(@"UnitsSoldCurrent", @"UnitsSoldCurrent"), new InputOutputColumnPair(@"UnitsSoldPrev", @"UnitsSoldPrev") })
                                    .Append(mlContext.Transforms.Concatenate(@"Features", new[] { @"CategoryId", @"UnitsSoldCurrent", @"UnitsSoldPrev" }))
                                    .Append(mlContext.Regression.Trainers.FastForest(new FastForestRegressionTrainer.Options() { NumberOfTrees = 574, NumberOfLeaves = 7, FeatureFraction = 0.6846322F, LabelColumnName = @"Next", FeatureColumnName = @"Features" }));

            return pipeline;
        }

        public static IEstimator<ITransformer> BuildPipelineForCategoryAvgPriceBaseModel(MLContext mlContext)
        {
            // Data process configuration with pipeline data transformations
            var pipeline = mlContext.Transforms.ReplaceMissingValues(new[] { new InputOutputColumnPair(@"CategoryId", @"CategoryId"), new InputOutputColumnPair(@"CategoryAvgPrice", @"CategoryAvgPrice"), new InputOutputColumnPair(@"UnitsSoldCurrent", @"UnitsSoldCurrent"), new InputOutputColumnPair(@"UnitsSoldPrev", @"UnitsSoldPrev") })
                                    .Append(mlContext.Transforms.Concatenate(@"Features", new[] { @"CategoryId", @"CategoryAvgPrice", @"UnitsSoldCurrent", @"UnitsSoldPrev" }))
                                    .Append(mlContext.Regression.Trainers.FastForest(new FastForestRegressionTrainer.Options() { NumberOfTrees = 4, NumberOfLeaves = 4, FeatureFraction = 1F, LabelColumnName = @"Next", FeatureColumnName = @"Features" }));
            return pipeline;
        }

        public static IEstimator<ITransformer> BuildPipelineForLocationBaseModel(MLContext mlContext)
        {
            // Data process configuration with pipeline data transformations
            var pipeline = mlContext.Transforms.ReplaceMissingValues(new[] { new InputOutputColumnPair(@"CountryId", @"CountryId"), new InputOutputColumnPair(@"UnitsSoldCurrent", @"UnitsSoldCurrent"), new InputOutputColumnPair(@"UnitsSoldPrev", @"UnitsSoldPrev") })
                                    .Append(mlContext.Transforms.Concatenate(@"Features", new[] { @"CountryId", @"UnitsSoldCurrent", @"UnitsSoldPrev" }))
                                    .Append(mlContext.Regression.Trainers.FastForest(new FastForestRegressionTrainer.Options() { NumberOfTrees = 574, NumberOfLeaves = 7, FeatureFraction = 0.6846322F, LabelColumnName = @"Next", FeatureColumnName = @"Features" }));

            return pipeline;
        }

        public static IEstimator<ITransformer> BuildPipelineForMonthBaseModel(MLContext mlContext)
        {
            // Data process configuration with pipeline data transformations
            var pipeline = mlContext.Transforms.ReplaceMissingValues(@"Month", @"Month")
                                    .Append(mlContext.Transforms.Concatenate(@"Features", new[] { @"Month" }))
                                    .Append(mlContext.Regression.Trainers.LightGbm(new LightGbmRegressionTrainer.Options() { NumberOfLeaves = 57, NumberOfIterations = 4, MinimumExampleCountPerLeaf = 20, LearningRate = 0.0223343096522008, LabelColumnName = @"Next", FeatureColumnName = @"Features", ExampleWeightColumnName = null, Booster = new GradientBooster.Options() { SubsampleFraction = 0.999999776672986, FeatureFraction = 0.99999999, L1Regularization = 4.05990598137366E-10, L2Regularization = 0.0152521608022869 }, MaximumBinCountPerFeature = 157 }));

            return pipeline;
        }
        #endregion

        #region Base Category Wise Model
        public static float TrainAndSaveCategoryWiseBaseModel(MLContext mlContext, List<CategoryBaseModelInputData> productSalesHistory, string outputModelPath)
        {
            var trainingDataView = mlContext.Data.LoadFromEnumerable(productSalesHistory);

            var trainingPipeline = BuildPipelineForCategoryBaseModel(mlContext);

            var crossValidationResults = mlContext.Regression.CrossValidate(data: trainingDataView, estimator: trainingPipeline, numberOfFolds: 6, labelColumnName: "Next");

            // metric evaluation 
            float metric = 0;
            foreach (var crossValidationResult in crossValidationResults)
            {
                metric += (float)crossValidationResult.Metrics.RSquared;
            }
            metric /= crossValidationResults.Count;

            // Train the model.
            var model = trainingPipeline.Fit(trainingDataView);

            // Save the model for later comsumption from end-user apps.
            mlContext.Model.Save(model, trainingDataView.Schema, outputModelPath);

            // return metric 
            return metric;
        }

        public static float PredictCategoryWiseBaseModel(MLContext mlContext, string outputModelPath, CategoryBaseModelInputData sampleData)
        {
            ITransformer mlModel = mlContext.Model.Load(outputModelPath, out var _);
            var predictEngine = mlContext.Model.CreatePredictionEngine<CategoryBaseModelInputData, CategoryBaseModelOutputData>(mlModel);
            var prediction = predictEngine.Predict(sampleData);
            return prediction.Score;
        }

        #endregion

        #region Base Location Wise Model
        public static float TrainAndSaveLocationWiseBaseModel(MLContext mlContext, List<LocationBaseModelInputData> productSalesHistory, string outputModelPath)
        {
            var trainingDataView = mlContext.Data.LoadFromEnumerable(productSalesHistory);

            var trainingPipeline = BuildPipelineForLocationBaseModel(mlContext);

            var crossValidationResults = mlContext.Regression.CrossValidate(data: trainingDataView, estimator: trainingPipeline, numberOfFolds: 2, labelColumnName: "Next");

            // metric evaluation 
            float metric = 0;
            foreach (var crossValidationResult in crossValidationResults)
            {
                metric += (float)crossValidationResult.Metrics.RSquared;
            }

            metric /= crossValidationResults.Count;

            // Train the model.
            var model = trainingPipeline.Fit(trainingDataView);

            // Save the model for later comsumption from end-user apps.
            mlContext.Model.Save(model, trainingDataView.Schema, outputModelPath);

            return metric;
        }

        public static float PredictLocationWiseBaseModel(MLContext mlContext, string outputModelPath, LocationBaseModelInputData sampleData)
        {
            ITransformer mlModel = mlContext.Model.Load(outputModelPath, out var _);
            var predictEngine = mlContext.Model.CreatePredictionEngine<LocationBaseModelInputData, LocationBaseModelOutputData>(mlModel);
            var prediction = predictEngine.Predict(sampleData);
            return prediction.Score;
        }
        #endregion

        #region Base Category Avg Price Wise Model
        public static float TrainAndSaveAveragePriceWiseBaseModel(MLContext mlContext, List<CategoryAvgPriceBaseModelInputData> productSalesHistory, string outputModelPath)
        {
            var trainingDataView = mlContext.Data.LoadFromEnumerable(productSalesHistory);

            var trainingPipeline = BuildPipelineForCategoryAvgPriceBaseModel(mlContext);

            var crossValidationResults = mlContext.Regression.CrossValidate(data: trainingDataView, estimator: trainingPipeline, numberOfFolds: 6, labelColumnName: "Next");

            // metric evaluation 
            float metric = 0;
            foreach (var crossValidationResult in crossValidationResults)
            {
                metric += (float)crossValidationResult.Metrics.RSquared;
            }
            metric /= crossValidationResults.Count;

            // Train the model.
            var model = trainingPipeline.Fit(trainingDataView);

            // Save the model for later comsumption from end-user apps.
            mlContext.Model.Save(model, trainingDataView.Schema, outputModelPath);

            return metric;
        }

        public static float PredictCategoryAvgPriceWiseBaseModel(MLContext mlContext, string outputModelPath, CategoryAvgPriceBaseModelInputData sampleData)
        {
            ITransformer mlModel = mlContext.Model.Load(outputModelPath, out var _);
            var predictEngine = mlContext.Model.CreatePredictionEngine<CategoryAvgPriceBaseModelInputData, CategoryAvgPriceBaseModelOutputData>(mlModel);
            var prediction = predictEngine.Predict(sampleData);
            return prediction.Score;
        }
        #endregion

        #region Base Month Wise Model
        public static float TrainAndSaveMonthWiseBaseModel(MLContext mlContext, List<MonthBaseModelInputData> productSalesHistory, string outputModelPath)
        {
            var trainingDataView = mlContext.Data.LoadFromEnumerable(productSalesHistory);

            var trainingPipeline = BuildPipelineForMonthBaseModel(mlContext);

            var crossValidationResults = mlContext.Regression.CrossValidate(data: trainingDataView, estimator: trainingPipeline, numberOfFolds: 6, labelColumnName: "Next");

            // metric evaluation 
            float metric = 0;
            foreach (var crossValidationResult in crossValidationResults)
            {
                metric += (float)crossValidationResult.Metrics.RSquared;
            }
            metric /= crossValidationResults.Count;

            // Train the model.
            var model = trainingPipeline.Fit(trainingDataView);

            // Save the model for later comsumption from end-user apps.
            mlContext.Model.Save(model, trainingDataView.Schema, outputModelPath);

            return metric;
        }

        public static float PredictMonthWiseBaseModel(MLContext mlContext, string outputModelPath, MonthBaseModelInputData sampleData)
        {
            ITransformer mlModel = mlContext.Model.Load(outputModelPath, out var _);
            var predictEngine = mlContext.Model.CreatePredictionEngine<MonthBaseModelInputData, MonthBaseModelOutputData>(mlModel);
            var prediction = predictEngine.Predict(sampleData);
            return prediction.Score;
        }
        #endregion

        #region Ensemble Model
        #endregion

        #region Individual Product Forcasting
        public static void TrainAndSaveIndividualProductModel(MLContext mlContext, List<ProductWeeklyTrainData> productSalesHistory, string outputModelPath)
        {
            var pipeline = mlContext.Transforms.ReplaceMissingValues(new[] { new InputOutputColumnPair(@"Month", @"Month"), new InputOutputColumnPair(@"Week", @"Week"), new InputOutputColumnPair(@"CurrentWeekQuantity", @"CurrentWeekQuantity") })
                                    .Append(mlContext.Transforms.Concatenate(@"Features", new[] { @"Month", @"Week", @"CurrentWeekQuantity" }))
                                    .Append(mlContext.Regression.Trainers.FastForest(new FastForestRegressionTrainer.Options() { NumberOfTrees = 1130, NumberOfLeaves = 4, FeatureFraction = 0.9370067F, LabelColumnName = @"Next", FeatureColumnName = @"Features" }));
            var trainingDataView = mlContext.Data.LoadFromEnumerable(productSalesHistory);
            var model = pipeline.Fit(trainingDataView);

            // Save the model for later comsumption from end-user apps.
            mlContext.Model.Save(model, trainingDataView.Schema, outputModelPath);

        }
        public static float PredictIndividualProductModel(MLContext mlContext, string outputModelPath, ProductWeeklyTrainData sampleData)
        {
            ITransformer mlModel = mlContext.Model.Load(outputModelPath, out var _);
            var predictEngine = mlContext.Model.CreatePredictionEngine<ProductWeeklyTrainData, IndividualModelOutput>(mlModel);
            var prediction = predictEngine.Predict(sampleData);
            return prediction.Score;
        }
        
        public static void TrainAndSaveIndividualProductMonthlyModel(MLContext mlContext, List<ProductMonthlyTrainData> productSalesHistory, string outputModelPath)
        {
            var pipeline = mlContext.Transforms.ReplaceMissingValues(new[] { new InputOutputColumnPair(@"Month", @"Month"), new InputOutputColumnPair(@"CurrentMonthQuantity", @"CurrentMonthQuantity") })
                                    .Append(mlContext.Transforms.Concatenate(@"Features", new[] { @"Month", @"CurrentMonthQuantity" }))
                                    .Append(mlContext.Regression.Trainers.FastForest(new FastForestRegressionTrainer.Options() { NumberOfTrees = 4, NumberOfLeaves = 4, FeatureFraction = 1F, LabelColumnName = @"Next", FeatureColumnName = @"Features" }));
            var trainingDataView = mlContext.Data.LoadFromEnumerable(productSalesHistory);
            var model = pipeline.Fit(trainingDataView);

            // Save the model for later comsumption from end-user apps.
            mlContext.Model.Save(model, trainingDataView.Schema, outputModelPath);
        }

        public static float PredictIndividualProductMonthlyModel(MLContext mlContext, string outputModelPath, ProductMonthlyTrainData sampleData)
        {
            ITransformer mlModel = mlContext.Model.Load(outputModelPath, out var _);
            var predictEngine = mlContext.Model.CreatePredictionEngine<ProductMonthlyTrainData, ModelMonthlyOutput>(mlModel);
            var prediction = predictEngine.Predict(sampleData);
            return prediction.Score;
        }
        #endregion
    }
}
