using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models
{
    public record GroupProductPredictionModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Admin.SalesForecasting.List.GroupProductPrediction.Table.ProductName")]
        public string ProductName { get; set; }
        [NopResourceDisplayName("Admin.SalesForecasting.List.GroupProductPrediction.Table.WeeklyUnitPrediction")]
        public int WeeklyUnitPrediction { get; set; }
        [NopResourceDisplayName("Admin.SalesForecasting.List.GroupProductPrediction.Table.MonthlyUnitPrediction")]
        public int MonthlyUnitPrediction { get;set; }

        [NopResourceDisplayName("Admin.SalesForecasting.List.GroupProductPrediction.Table.WeeklyMonetaryPrediction")]
        public float WeeklyMonetaryPrediction { get; set; }
        [NopResourceDisplayName("Admin.SalesForecasting.List.GroupProductPrediction.Table.MonthlyMonetaryPrediction")]
        public float MonthlyMonetaryPrediction { get; set; }

    }
}
