using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NopStation.Plugin.Misc.SalesForecasting.Models.DataPreprocessingModels
{
    public class LocationSpecificTempData
    {
        public int? BillingAddressId { get; set; }
        public float? OrderTotal { get; set; }
        public DateTime? OrderDate { get; set; }
        public int? CountryId { get; set; }
        public float? LocationTotalSales { get; set; }
    }

    public class LocationSpecificData
    {
        public float LocationAvgSales { get; set; }
        public int CountryId { get; set; }

    }

}
