using System;
using System.Collections.Generic;
using System.Text;
using Nop.Web.Framework.Models;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models
{
    public class InventoryProductWeeklyPrediction
    {
        public int WeekId { get; set; }
        public string WeekName { get; set; }
        public int WeeklySalesQuantity { get; set; }
        public List<WeeklyProduct> WeeklyProducts { get; set; }
    }

    public partial record WeeklyProduct : BaseNopEntityModel
    {
        public int ProductId { get; set; }
        public string ProductName { set; get; }
        public int SalesQuantity { get; set; }
    }
}
