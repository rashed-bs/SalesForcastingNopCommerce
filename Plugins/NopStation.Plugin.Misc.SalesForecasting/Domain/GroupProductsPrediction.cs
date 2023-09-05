using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core;

namespace NopStation.Plugin.Misc.SalesForecasting.Domain
{
    public class GroupProductsPrediction : BaseEntity
    {
        public int ProductGroupId { get; set; }
        public int ProductId { get; set; }
        public int WeeklyUnitPrediction { get; set; }
        public int MonthlyUnitPrediction { get; set; }
        public float WeeklyMonetaryPrediction { get; set; }
        public float MonthlyMonetaryPrediction { get; set; }
    }
}
