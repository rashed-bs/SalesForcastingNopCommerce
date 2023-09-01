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
                NumberofBestDemandProduct=100
            };
           await _settingService.SaveSettingAsync(settings);

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
                Title = await _localizationService.GetResourceAsync("Admin.NopStation.SalesForecasting.Menu.SalesForecasting"),
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
                    Title = await _localizationService.GetResourceAsync("Admin.NopStation.SalesForecasting.Menu.Configuration"),
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
                    Title = await _localizationService.GetResourceAsync("Admin.NopStation.SalesForecasting.Menu.SalesPrediction"),
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
                    Title = await _localizationService.GetResourceAsync("Admin.NopStation.SalesForecasting.Menu.ProductPrediction"),
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