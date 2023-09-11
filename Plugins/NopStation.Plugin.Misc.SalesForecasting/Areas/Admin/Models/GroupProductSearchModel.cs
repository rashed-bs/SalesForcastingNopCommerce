using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models
{
    public record GroupProductSearchModel : BaseSearchModel
    {
        [NopResourceDisplayName("Admin.SalesForecasting.List.GroupSearchModel.GroupName")]
        public string GroupName { get; set; }
        [NopResourceDisplayName("Admin.SalesForecasting.List.GroupSearchModel.IsActive")]
        public bool IsActive { get; set; }
    }
}
