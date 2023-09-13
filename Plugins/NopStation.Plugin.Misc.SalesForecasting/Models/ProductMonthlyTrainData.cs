using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.Data;

namespace NopStation.Plugin.Misc.SalesForecasting.Models
{
    public class ProductMonthlyTrainData
    {
        public float Month { get; set; }
        public float CurrentMonthQuantity { get; set; }
        public float Next { get; set; }
    }

    public class ModelMonthlyOutput
    {
        [ColumnName(@"Month")]
        public float Month { get; set; }

        [ColumnName(@"CurrentMonthQuantity")]
        public float CurrentMonthQuantity { get; set; }

        [ColumnName(@"Next")]
        public float Next { get; set; }

        [ColumnName(@"Features")]
        public float[] Features { get; set; }

        [ColumnName(@"Score")]
        public float Score { get; set; }

    }
}
