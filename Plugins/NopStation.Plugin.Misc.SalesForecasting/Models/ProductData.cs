using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NopStation.Plugin.Misc.SalesForecasting.Models
{
    public class ProductData
    {
        public float CategoryId { get; set; }
        public float CountryId { get; set; }
        public float IsWeekend { get; set; }
        public float DiscountRate { get; set; }
        public float CategoryAvgPrice { get; set; }
        public float Month { get; set; }
        public float Day { get; set; }
        public float Units { get; set; }
    }
}
