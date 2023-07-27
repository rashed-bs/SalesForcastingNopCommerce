using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models
{
    public partial record PredictionSearchModel : BaseSearchModel
    {
        public PredictionSearchModel()
        {
            AvailableCategory = new List<SelectListItem>();
            AvailableWeekItem = new List<SelectListItem>();
        }

        public int CategoryId { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal ShippingCharge { get; set; }
        public int WeekId { set; get; }

        public IList<SelectListItem> AvailableCategory { get; set; }
        public IList<SelectListItem> AvailableWeekItem { get; set; }
    }
}
