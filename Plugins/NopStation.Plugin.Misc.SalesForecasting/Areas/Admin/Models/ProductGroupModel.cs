using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Web.Framework.Models;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models
{
    public record ProductGroupModel : BaseNopEntityModel
    {
        public ProductGroupModel()
        {
            RelatedProductGroupSearchModel = new RelatedProductGroupSearchModel();
        }
        public string GroupName { get; set; }
        public bool IsActive { get; set; }

        public bool DiscountAppliedFrequently { get; set; }

        public RelatedProductGroupSearchModel RelatedProductGroupSearchModel { get; set; }
        public GroupProductPredictionSearchModel GroupProductPredictionSearchModel { get; set; }
    }
}
