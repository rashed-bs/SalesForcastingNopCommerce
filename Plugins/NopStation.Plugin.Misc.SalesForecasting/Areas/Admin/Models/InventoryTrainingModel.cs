using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ML.Data;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models
{
    public class InventoryTrainingModel
    {
        public float ProductId { get; set; }
        public float ShowOnHomepage { get; set; }
        public float WeekNo { get; set; }
        public float UnitPrice { get; set; }
        public float DiscountAmount { get; set; }
        public float OrderShippingCharge { get; set; }
        public float SalesQuantity { get; set; }
    }
}
