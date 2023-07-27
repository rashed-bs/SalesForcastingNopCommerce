﻿namespace NopStation.Plugin.Misc.SalesForecasting.Models
{
    /// <summary>
    /// This is the output of the scored regression model, the prediction.
    /// </summary>
    public class SalesRegressionPrediction
    {
        // Below columns are produced by the model's predictor.
        public float Score { get; set; }
    }

    /// <summary>
    /// This is the output of the scored time series model, the prediction.
    /// </summary>
    public class SalesTimeSeriesPrediction
    {
        public float[] ForecastedProductUnits { get; set; }

        public float[] ConfidenceLowerBound { get; set; }

        public float[] ConfidenceUpperBound { get; set; }
    }
}