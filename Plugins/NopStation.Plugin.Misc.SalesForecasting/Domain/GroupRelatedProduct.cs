using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core;

namespace NopStation.Plugin.Misc.SalesForecasting.Domain
{
    public class GroupRelatedProduct : BaseEntity
    {
        public string Product1 { get; set; }
        public string Product2 { get; set; }
        public int DisplayOrder { get; set; }

    }
}
