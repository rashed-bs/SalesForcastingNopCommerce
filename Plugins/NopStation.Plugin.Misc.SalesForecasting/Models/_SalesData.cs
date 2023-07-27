using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NopStation.Plugin.Misc.SalesForecasting.Models
{
    public class _SalesData
    {
        [LoadColumn(0)]
        public float PreviousDayUnits { get; set; }
        [LoadColumn(1)]
        public float Year { get; set; }
        [LoadColumn(2)]
        public float Month { get; set; }
        [LoadColumn(3)]
        public float Day { get; set; }
        [LoadColumn(4)]
        public float CategoryID { get; set; }
        [LoadColumn(5)]
        public float PercentageDiscount { get; set; }
        [LoadColumn(6)]
        public float ShippingCharge { get; set; }

        [LoadColumn(7)]
        public float NextDayUnits { get; set; }

        [LoadColumn(8)]
        public float IsWeekend { get; set; }
    }
}
