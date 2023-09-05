using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Web.Framework.Models;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models
{
    public record AddRelatedProductGroupModel : BaseNopModel
    {
        #region Ctor

        public AddRelatedProductGroupModel()
        {
            SelectedProductIds = new List<int>();
        }
        #endregion

        #region Properties

        public int ProductGroupId { get; set; }

        public IList<int> SelectedProductIds { get; set; }

        #endregion
    }
}
