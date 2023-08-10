using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models
{
    public class TestFeatureModel
    {
        public int CategoryId { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal ShippingCharge { get; set; }
        public int LocationId { get; set; }
    }

    public class JsonResponse
    {
        public string XLabelName { get; set; }
        public float YLabelValue { get; set; }

        public float? YLabelValuePredicted { get; set; }

        public float? YLabelValueInStock { get; set; }

    }
}
