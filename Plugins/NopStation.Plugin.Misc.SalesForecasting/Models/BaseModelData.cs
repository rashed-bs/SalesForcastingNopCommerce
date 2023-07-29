using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NopStation.Plugin.Misc.SalesForecasting.Models
{
    public class CategoryBaseModelData
    {
        public int CategoryId { get; set; }
        public float UnitsSoldCurrent { get; set; }
        public float UnitsSoldPrev { get; set; }
        public float Next { get; set; }
    }

    public class CategoryAvgPriceBaseModelData
    {
        public int CategoryId { get; set; }
        public float CategoryAvgPrice { get; set; }
        public float UnitsSoldCurrent { get; set; }
        public float UnitsSoldPrev { get; set; }
        public float Next { get; set; }
    }

    public class LocationBaseModelData
    {
        public int CountryId { get; set; }
        public float UnitsSoldCurrent { get; set; }
        public float UnitsSoldPrev { get; set; }
        public float Next { get; set; }
    }

    public class TemporaryBaseModelData
    {
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? CategoryId { get; set; }
        public int? CountryId { get; set; }
        public float? OrderTotal { get; set; }
        public float? Unit { get; set; }
        public float? Avg { get; set; }
        public float? Max { get; set; }
        public float? Min { get; set; }
        public float? CategoryAvgPrice { get; set; }
    }
}
