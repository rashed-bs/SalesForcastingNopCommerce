using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;
using System.ComponentModel.DataAnnotations;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models
{
    public record ConfigurationModel : BaseNopModel, ISettingsModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Admin.NopStation.SalesForecasting.Fields.NumberofBestDemandProduct")]
        public int NumberofBestDemandProduct { get; set; }
        public bool NumberofBestDemandProduct_OverrideForStore { get; set; }
        [NopResourceDisplayName("Admin.SalesForecasting.Configure.MondayIsWeekend")]
        public bool MondayIsWeekend { get; set; }
        public bool MondayIsWeekend_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.SalesForecasting.Configure.TuesdayIsWeekend")]
        public bool TuesdayIsWeekend { get; set; }
        public bool TuesdayIsWeekend_OverrideForStore { get; set; }
        [NopResourceDisplayName("Admin.SalesForecasting.Configure.WednesdayIsWeekend")]
        public bool WednesdayIsWeekend { get; set; }
        public bool WednesdayIsWeekend_OverrideForStore { get; set; }
        [NopResourceDisplayName("Admin.SalesForecasting.Configure.ThursdayIsWeekend")]
        public bool ThursdayIsWeekend { get; set; }
        public bool ThursdayIsWeekend_OverrideForStore { get; set; }
        [NopResourceDisplayName("Admin.SalesForecasting.Configure.FridayIsWeekend")]
        public bool FridayIsWeekend { get; set; }
        public bool FridayIsWeekend_OverrideForStore { get; set; }
        [NopResourceDisplayName("Admin.SalesForecasting.Configure.SaturdayIsWeekend")]
        public bool SaturdayIsWeekend { get; set; }
        public bool SaturdayIsWeekend_OverrideForStore { get; set; }
        [NopResourceDisplayName("Admin.SalesForecasting.Configure.SundayIsWeekend")]
        public bool SundayIsWeekend { get; set; }
        public bool SundayIsWeekend_OverrideForStore { get; set; }
    }
}