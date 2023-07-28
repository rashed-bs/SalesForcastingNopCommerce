namespace NopStation.Plugin.Misc.SalesForecasting.Models
{
    public class ProductUnitRegressionPrediction
    {
        // Below columns are produced by the model's predictor.
        public float Score { get; set; }
    }
    public class ProductUnitTimeSeriesPrediction
    {
        public float[] ForecastedProductUnits { get; set; }

        public float[] ConfidenceLowerBound { get; set; }

        public float[] ConfidenceUpperBound { get; set; }
    }
}
