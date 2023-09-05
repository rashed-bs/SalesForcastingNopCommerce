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
        public bool MondayIsWeekend { get; set; }
        public bool MondayIsWeekend_OverrideForStore { get; set; }
        public bool TuesdayIsWeekend { get; set; }
        public bool TuesdayIsWeekend_OverrideForStore { get; set; }
        public bool WednesdayIsWeekend { get; set; }
        public bool WednesdayIsWeekend_OverrideForStore { get; set; }
        public bool ThursdayIsWeekend { get; set; }
        public bool ThursdayIsWeekend_OverrideForStore { get; set; }
        public bool FridayIsWeekend { get; set; }
        public bool FridayIsWeekend_OverrideForStore { get; set; }
        public bool SaturdayIsWeekend { get; set; }
        public bool SaturdayIsWeekend_OverrideForStore { get; set; }
        public bool SundayIsWeekend { get; set; }
        public bool SundayIsWeekend_OverrideForStore { get; set; }
    }
}