using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models
{
    public record ProductGroupModel : BaseNopEntityModel
    {
        public ProductGroupModel()
        {
            RelatedProductGroupSearchModel = new RelatedProductGroupSearchModel();
        }
        [NopResourceDisplayName("Admin.SalesForecasting.Create.Info.GroupName")]
        public string GroupName { get; set; }
        [NopResourceDisplayName("Admin.SalesForecasting.Create.Info.IsActive")]
        public bool IsActive { get; set; }
        [NopResourceDisplayName("Admin.SalesForecasting.Create.Info.DiscountAppliedFrequently")]
        public bool DiscountAppliedFrequently { get; set; }

        public RelatedProductGroupSearchModel RelatedProductGroupSearchModel { get; set; }
        public GroupProductPredictionSearchModel GroupProductPredictionSearchModel { get; set; }
    }
}
