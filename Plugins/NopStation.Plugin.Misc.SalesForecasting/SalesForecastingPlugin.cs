using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework.Menu;
using NopStation.Plugin.Misc.Core;
using NopStation.Plugin.Misc.Core.Services;
using NopStation.Plugin.Misc.SalesForecasting.Extensions;

namespace NopStation.Plugin.Misc.SalesForecasting
{
    /// <summary>
    /// Manual payment processor
    /// </summary>
    public class SalesForecastingPlugin : BasePlugin, IAdminMenuPlugin, IMiscPlugin, INopStationPlugin
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly IPermissionService _permissionService;
        private readonly INopStationCoreService _nopStationCoreService;

        #endregion

        #region Ctor

        public SalesForecastingPlugin(ILocalizationService localizationService,
            ISettingService settingService,
            IWebHelper webHelper,
            IPermissionService permissionService,
            INopStationCoreService nopStationCoreService)
        {
            _localizationService = localizationService;
            _settingService = settingService;
            _webHelper = webHelper;
            _permissionService = permissionService;
            _nopStationCoreService = nopStationCoreService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/SalesForecasting/Configure";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override async Task InstallAsync()
        {
            await this.InstallPluginAsync(new SalesForecastingProvider());

            //settings
            var settings = new SalesForecastingSettings
            {
                NumberofBestDemandProduct = 100
            };
            await _settingService.SaveSettingAsync(settings);

            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Admin.SalesForecasting.Menu.SalesForecasting"] = "Sales prediction",
                ["Admin.SalesForecasting.Menu.Configuration"] = "Configuration",
                ["Admin.SalesForecasting.Menu.SalesPrediction"] = "Overall prediction",
                ["Admin.SalesForecasting.Menu.ProductPrediction"] = "Product prediction",
                ["Admin.SalesForecasting.Configure.PageTitle"] = "Configure",
                ["Admin.SalesForecasting.Configure.PageHead"] = "Configure sales forcasting",
                ["Admin.SalesForecasting.Configure.MondayIsWeekend"] = "Monday Is Weekend", 
                ["Admin.SalesForecasting.Configure.MondayIsWeekend.Hint"] = "Check if monday is weekend in this sore region", 
                ["Admin.SalesForecasting.Configure.TuesdayIsWeekend"] = "Tuesday Is Weekend",
                ["Admin.SalesForecasting.Configure.TuesdayIsWeekend.Hint"] = "Check if tuesday is weekend in this sore region",
                ["Admin.SalesForecasting.Configure.WednesdayIsWeekend"] = "Wednesday Is Weekend",
                ["Admin.SalesForecasting.Configure.WednesdayIsWeekend.Hint"] = "Check if wednesday is weekend in this sore region",
                ["Admin.SalesForecasting.Configure.ThursdayIsWeekend"] = "Thursday Is Weekend",
                ["Admin.SalesForecasting.Configure.ThursdayIsWeekend.Hint"] = "Check if thursday is weekend in this sore region",
                ["Admin.SalesForecasting.Configure.FridayIsWeekend"] = "Friday Is Weekend",
                ["Admin.SalesForecasting.Configure.FridayIsWeekend.Hint"] = "Check if friday is weekend in this sore region",
                ["Admin.SalesForecasting.Configure.SaturdayIsWeekend"] = "Saturday Is Weekend",
                ["Admin.SalesForecasting.Configure.SaturdayIsWeekend.Hint"] = "Check if saturday is weekend in this sore region",
                ["Admin.SalesForecasting.Configure.SundayIsWeekend"] = "Sunday Is Weekend",
                ["Admin.SalesForecasting.Configure.SundayIsWeekend.Hint"] = "Check if sunday is weekend in this sore region",
                ["Admin.SalesForecasting.Configure.TrainModel"] = "Train",
                ["Admin.SalesForecasting.Configure.StoreConfigure"] = "Set up store regional information",
                ["Admin.SalesForecasting.Create.AddNew"] = "Add",
                ["Admin.SalesForecasting.Create.PageTitle"] = "Add new group",
                ["Admin.SalesForecasting.Create.Info"] = "Group information",
                ["Admin.SalesForecasting.Create.Info.GroupName"] = "Group Name",
                ["Admin.SalesForecasting.Create.Info.GroupName.Hint"] = "Give an appropriate group name",
                ["Admin.SalesForecasting.Create.Info.IsActive"] = "Active",
                ["Admin.SalesForecasting.Create.Info.IsActive.Hint"] = "Uncheck incase you want this group inactive. Unchecking will make this as if it wasn't created at all.",
                ["Admin.SalesForecasting.Create.Info.DiscountAppliedFrequently"] = "Discount",
                ["Admin.SalesForecasting.Create.Info.DiscountAppliedFrequently.Hint"] = "You apply discounts to the product groups frequently.",
                ["Admin.SalesForecasting.Create.RelatedProducts"] = "Related products",
                ["Admin.SalesForecasting.Create.RelatedProducts.SaveBeforeEdit"] = "Save the group before adding products to the group.",
                ["Admin.SalesForecasting.Create.RelatedProducts.AddNew"] = "Add new Related product",
                ["Admin.SalesForecasting.Create.RelatedProducts.Hint"] = "You can add as much as you want. We recommend that you add only add similiar types of products in a single group for maintaining purposes. Please note that, always train and forcast to get the latest prediction. Training from the main configure page will remove all the group prediction history.",
                ["Admin.SalesForecasting.Create.BackToList"] = "Back to product group list",
                ["Admin.SalesForecasting.Create.AddPopUp.PageTitle"] = "Add a product",
                ["Admin.SalesForecasting.Create.AddPopUp.AddNew"] = "Add a product",
                ["Admin.SalesForecasting.Edit.BackToList"] = "Back to product group list",
                ["Admin.SalesForecasting.Edit.pageTitle"] = "Edit group details",
                ["Admin.SalesForecasting.Edit.pageHead"] = "Edit group details",
                ["Admin.SalesForecasting.List.pageTitle"] = "Product groups",
                ["Admin.SalesForecasting.List"] = "Product groups",
                ["Admin.SalesForecasting.List.GroupSearchModel.GroupName"] = "Group name",
                ["Admin.SalesForecasting.List.GroupSearchModel.GroupName.Hint"] = "Search by group name",
                ["Admin.SalesForecasting.List.GroupSearchModel.IsActive"] = "Active",
                ["Admin.SalesForecasting.List.GroupSearchModel.IsActive.Hint"] = "Check to filter all active groups",
                ["Admin.SalesForecasting.List.GroupProductPrediction.Table.ProductName"] = "Product",
                ["Admin.SalesForecasting.List.GroupProductPrediction.Table.WeeklyUnitPrediction"] = "Weekly Prediction (Unit)",
                ["Admin.SalesForecasting.List.GroupProductPrediction.Table.WeeklyMonetaryPrediction"] = "Weekly Prediction (Money)",
                ["Admin.SalesForecasting.List.GroupProductPrediction.Table.MonthlyUnitPrediction"] = "Monthly Prediction (Unit)",
                ["Admin.SalesForecasting.List.GroupProductPrediction.Table.MonthlyMonetaryPrediction"] = "Monthly Prediction (Money)",
                ["Admin.SalesForecasting.SalesPrediction.Weekly.Chart.Title"] = "Weekly sales prediction",
                ["Admin.SalesForecasting.SalesPrediction.Monthly.Chart.Title"] = "Monthly top categories sales prediction",
                ["Admin.SalesForecasting.SalesPrediction.Chart.Contribution.Title"] = "Sales contribution of categories (monthly)",
                ["Admin.SalesForecasting.SalesPrediction.Chart.SalesVsStock.Title"] = "Category sales vs stock (monthly)",
                ["Plugins.NopStation.InventoryPrediction.Report.WeeklySalesQuantity"] = "Weekly Sales Quantity",
                ["Plugins.NopStation.InventoryPrediction.Report.MonthlySalesQuantity"] = "Monthly Sales Quantity",
            });

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override async Task UninstallAsync()
        {
            await this.UninstallPluginAsync(new SalesForecastingProvider());

            //settings
            await _settingService.DeleteSettingAsync<SalesForecastingSettings>();

            await base.UninstallAsync();
        }

        public async Task ManageSiteMapAsync(SiteMapNode rootNode)
        {
            var menu = new SiteMapNode()
            {
                Title = await _localizationService.GetResourceAsync("Admin.SalesForecasting.Menu.SalesForecasting"),
                Visible = true,
                IconClass = "far fa-dot-circle",
            };

            if (await _permissionService.AuthorizeAsync(SalesForecastingProvider.ManageConfiguration))
            {
                var settings = new SiteMapNode()
                {
                    Visible = true,
                    IconClass = "far fa-circle",
                    Url = "~/Admin/SalesForecasting/Configure",
                    Title = await _localizationService.GetResourceAsync("Admin.SalesForecasting.Menu.Configuration"),
                    SystemName = "SalesForecasting.Configuration"
                };
                menu.ChildNodes.Add(settings);
            }

            if (await _permissionService.AuthorizeAsync(SalesForecastingProvider.ViewSalesForecasting))
            {
                var settings = new SiteMapNode()
                {
                    Visible = true,
                    IconClass = "far fa-circle",
                    Url = "~/Admin/SalesForecasting/SalesPrediction",
                    Title = await _localizationService.GetResourceAsync("Admin.SalesForecasting.Menu.SalesPrediction"),
                    SystemName = "SalesForecasting.SalesPrediction"
                };
                menu.ChildNodes.Add(settings);
            }

            if (await _permissionService.AuthorizeAsync(SalesForecastingProvider.ViewSalesForecasting))
            {
                var settings = new SiteMapNode()
                {
                    Visible = true,
                    IconClass = "far fa-circle",
                    Url = "~/Admin/ProductGroup/Index",
                    Title = await _localizationService.GetResourceAsync("Admin.SalesForecasting.Menu.ProductPrediction"),
                    SystemName = "SalesForecasting.ProductPrediction"
                };
                menu.ChildNodes.Add(settings);
            }

            await _nopStationCoreService.ManageSiteMapAsync(rootNode, menu, NopStationMenuType.Plugin);
        }

        public List<KeyValuePair<string, string>> PluginResouces()
        {
            var list = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Admin.NopStation.SalesForecasting.Instructions", "Fill the from value meticulous to get best performance"),
                new KeyValuePair<string, string>("Admin.NopStation.SalesForecasting.Model.Instructions", "Train the model to get better results if you have new data"),
                new KeyValuePair<string, string>("Admin.NopStation.SalesForecasting.Fields.NumberofBestDemandProduct", "Number Of Best Demand Product"),
                new KeyValuePair<string, string>("Admin.NopStation.SalesForecasting.Fields.NumberofBestDemandProduct.Hint", "Enter number of best demand product to see in the list"),
                new KeyValuePair<string, string>("Admin.NopStation.SalesForecasting.Configure.TrainModel", "Train Model"),
                new KeyValuePair<string, string>("Admin.NopStation.SalesForecasting.Configure.TrainModel.Message", "Train model successfully with new data"),
                new KeyValuePair<string, string>("Admin.NopStation.SalesForecasting.Report.WeeklySalesQuantity", "Sales Quantity"),
                new KeyValuePair<string, string>("Admin.NopStation.SalesForecasting.Report.PredictionStatistics", "Prediction Statistics"),
                new KeyValuePair<string, string>("Admin.NopStation.SalesForecasting.Report.WeeklySearch", "Weekly Search"),
                new KeyValuePair<string, string>("Admin.NopStation.SalesForecasting.Report.Table.ProductName", "Product Name"),
                new KeyValuePair<string, string>("Admin.NopStation.SalesForecasting.Report.Table.SalesQuantity", "Sales Quantity"),
                new KeyValuePair<string, string>("Admin.NopStation.SalesForecasting.Menu.MenuItem.InventoryPrediction", "Inventory Prediction"),
                new KeyValuePair<string, string>("Admin.NopStation.SalesForecasting.Menu.MenuItem.DataVisualization", "Data Visualization")
            };

            return list;
        }

        #endregion
    }
}