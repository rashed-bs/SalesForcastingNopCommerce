using Nop.Core.Configuration;

namespace NopStation.Plugin.Misc.SalesForecasting
{
    /// <summary>
    /// Represents settings of manual payment plugin
    /// </summary>
    public class SalesForecastingSettings : ISettings
    {
        public int NumberofBestDemandProduct { get; set; }
        public bool MondayIsWeekend { get; set; }
        public bool TuesdayIsWeekend { get; set; }
        public bool WednesdayIsWeekend { get; set; }
        public bool ThursdayIsWeekend { get; set; }
        public bool FridayIsWeekend { get; set; }
        public bool SaturdayIsWeekend { get; set; }
        public bool SundayIsWeekend { get; set; }
    }
}
