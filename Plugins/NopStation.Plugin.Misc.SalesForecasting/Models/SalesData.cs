using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NopStation.Plugin.Misc.SalesForecasting.Models
{
    public class SalesData
    {
        public float PreviousDayUnits { get; set; }
        public float Year { get; set; }
        public float Month { get; set; }
        public float Day { get; set; }
        public float CategoryID { get; set; }
        public float PercentageDiscount { get; set; }
        public float ShippingCharge { get; set; }
        public float NextDayUnits { get; set; }
        public float IsWeekend { get; set; }
    }

    public class LiteSalesData
    {
        public DateTime Date { get; set; }
        public float unitSold { get; set; }
    }
}
