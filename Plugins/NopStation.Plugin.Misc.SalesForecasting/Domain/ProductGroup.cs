using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core;

namespace NopStation.Plugin.Misc.SalesForecasting.Domain
{
    public class ProductGroup : BaseEntity
    {
        public string GroupName { get; set; }
        public bool IsActive { get; set; }
        public bool DiscountAppliedFrequently { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
    }
}
