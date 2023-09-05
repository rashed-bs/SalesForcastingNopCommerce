using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.ML.Data;

namespace NopStation.Plugin.Misc.SalesForecasting.Models
{
    public class TempProductWeeklyTrainData
    {
        public float Year { get; set; }
        public float Month { get; set; }
        public float Week { get; set; }
        public float CurrentWeekQuantity { get; set; }
        public float Next { get; set; }
    }
    
    public class ProductWeeklyTrainData
    {
        public float Month { get; set; }
        public float Week { get; set; }
        public float CurrentWeekQuantity { get; set; }
        public float Next { get; set; }
    }

    public class ProductDailyGroupedTrainData
    {
        public float Year { get; set; }
        public float Month { get; set; }
        public float Day { get; set; }
        public float CurrentWeekQuantity { get; set; }
    }

    public class IndividualModelInput
    {
        [ColumnName(@"Month")]
        public float Month { get; set; }

        [ColumnName(@"Week")]
        public float Week { get; set; }

        [ColumnName(@"CurrentWeekQuantity")]
        public float CurrentWeekQuantity { get; set; }

        [ColumnName(@"Next")]
        public float Next { get; set; }

    }


    public class IndividualModelOutput
    {
        [ColumnName(@"Month")]
        public float Month { get; set; }

        [ColumnName(@"Week")]
        public float Week { get; set; }

        [ColumnName(@"CurrentWeekQuantity")]
        public float CurrentWeekQuantity { get; set; }

        [ColumnName(@"Next")]
        public float Next { get; set; }

        [ColumnName(@"Features")]
        public float[] Features { get; set; }

        [ColumnName(@"Score")]
        public float Score { get; set; }

    }
}
