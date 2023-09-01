using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Web.Framework.Models;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models
{
    public record GroupProductPredictionModel : BaseNopEntityModel
    {
        public string ProductName { get; set; }
        public int WeeklyUnitPrediction { get; set; }
        public int MonthlyUnitPrediction { get;set; }
        public float WeeklyMonetaryPrediction { get; set; }
        public float MonthlyMonetaryPrediction { get;
        }

    }
}
