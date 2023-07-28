using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NopStation.Plugin.Misc.SalesForecasting.Models
{

    public class SalesRegressionPrediction
    {
        // Below columns are produced by the model's predictor.
        public float Score { get; set; }
    }

    public class WeeklySalesTimeSeriesPrediction
    {
        public float[] ForecastedWeeklySalesUnits { get; set; }

        public float[] ConfidenceLowerBound { get; set; }

        public float[] ConfidenceUpperBound { get; set; }
    }
}
