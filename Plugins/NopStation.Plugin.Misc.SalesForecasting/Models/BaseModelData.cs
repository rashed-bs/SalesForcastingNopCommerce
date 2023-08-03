using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.Data;

namespace NopStation.Plugin.Misc.SalesForecasting.Models
{
    public class CategoryBaseModelInputData
    {
        public float CategoryId { get; set; }
        public float UnitsSoldCurrent { get; set; }
        public float UnitsSoldPrev { get; set; }
        public float Next { get; set; }
    }

    public class CategoryBaseModelOutputData
    {
        public float CategoryId { get; set; }

        public float UnitsSoldCurrent { get; set; }

        public float UnitsSoldPrev { get; set; }

        public float Next { get; set; }

        public float[] Features { get; set; }

        public float Score { get; set; }

    }

    public class CategoryAvgPriceBaseModelInputData
    {
        public float CategoryId { get; set; }
        public float CategoryAvgPrice { get; set; }
        public float UnitsSoldCurrent { get; set; }
        public float UnitsSoldPrev { get; set; }
        public float Next { get; set; }
    }

    public class CategoryAvgPriceBaseModelOutputData
    {
        public float CategoryId { get; set; }

        public float CategoryAvgPrice { get; set; }

        public float UnitsSoldCurrent { get; set; }

        public float UnitsSoldPrev { get; set; }

        public float Next { get; set; }

        public float[] Features { get; set; }

        public float Score { get; set; }

    }

    public class LocationBaseModelInputData
    {
        public float CountryId { get; set; }
        public float UnitsSoldCurrent { get; set; }
        public float UnitsSoldPrev { get; set; }
        public float Next { get; set; }
    }

    public class LocationBaseModelOutputData
    {
        public float CountryId { get; set; }

        public float UnitsSoldCurrent { get; set; }

        public float UnitsSoldPrev { get; set; }

        public float Next { get; set; }

        public float[] Features { get; set; }

        public float Score { get; set; }

    }

    public class MonthBaseModelInputData
    {
        public float Month { get; set; }
        public float UnitsSoldCurrent { get; set; }
        public float UnitsSoldPrev { get; set; }
        public float Next { get; set; }
    }

    public class MonthBaseModelOutputData
    {
        public float Month { get; set; }

        public float UnitsSoldCurrent { get; set; }

        public float UnitsSoldPrev { get; set; }

        public float Next { get; set; }

        public float[] Features { get; set; }

        public float Score { get; set; }

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

    public class MonthlySalesCategoryContribution
    {
        public float CategoryId { get; set; }
        public string CategoryName { get; set; }
        public float contribution { get; set; } // percentage of total sales 
        public float quantity { get; set; }
    }
}