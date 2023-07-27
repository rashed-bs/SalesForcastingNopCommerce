using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using NopStation.Plugin.Misc.Core.Controllers;
using NopStation.Plugin.Misc.Core.Filters;
using NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Factories;
using NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models;
using NopStation.Plugin.Misc.SalesForecasting.Extensions;
using NopStation.Plugin.Misc.SalesForecasting.Services;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Controllers
{
    public class SalesForecastingController : NopStationAdminController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly ISalesForecastingService _salesForecastingService;
        private readonly IProductService _productService;
        private readonly ISalesForecastingModelFactory _salesForecastingModelFactory;

        #endregion

        #region Ctor

        public SalesForecastingController(ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext,
            ISalesForecastingService salesForecastingService,
            IProductService productService,
            ISalesForecastingModelFactory salesForecastingModelFactory)
        {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _storeContext = storeContext;
            _salesForecastingService = salesForecastingService;
            _productService = productService;
            _salesForecastingModelFactory = salesForecastingModelFactory;
        }

        #endregion

        #region Methods
        
        public async Task<IActionResult> ConfigureAsync()
        {
            if (!await _permissionService.AuthorizeAsync(SalesForecastingProvider.ManageConfiguration))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var checkOutSettings = await _settingService.LoadSettingAsync<SalesForecastingSettings>(storeScope);

            var model = new ConfigurationModel
            {
                NumberofBestDemandProduct = checkOutSettings.NumberofBestDemandProduct
            };

            if (storeScope > 0)
                model.NumberofBestDemandProduct_OverrideForStore = await _settingService.SettingExistsAsync(checkOutSettings, x => x.NumberofBestDemandProduct, storeScope);

            return View(model);
        }

        [EditAccess, HttpPost]
        public async Task<IActionResult> ConfigureAsync(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(SalesForecastingProvider.ManageConfiguration))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await ConfigureAsync();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var checkOutSettings = await _settingService.LoadSettingAsync<SalesForecastingSettings>(storeScope);

            //save settings
            checkOutSettings.NumberofBestDemandProduct = model.NumberofBestDemandProduct;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */

            await _settingService.SaveSettingOverridablePerStoreAsync(checkOutSettings, x => x.NumberofBestDemandProduct, model.NumberofBestDemandProduct_OverrideForStore, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await ConfigureAsync();
        }

        [EditAccess, HttpPost]
        public async Task<IActionResult> TrainModelAsync()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            var response = await _salesForecastingService.TrainAndTestModelAsync();

            if (response.Item1)
                _notificationService.SuccessNotification("Training successfull");
            else
               _notificationService.ErrorNotification("Training failed");

            return RedirectToAction("Configure");
        }

        public async Task<IActionResult> SalesPrediction()
        {
            var searchModel = await _salesForecastingModelFactory.PreparePredictionSearchModel(new PredictionSearchModel());
            return View(searchModel);
        }

        public async Task<IActionResult> SalesHistoryAsync()
        {
            var model = await _salesForecastingService.GetSalesHistoryDataAsync();
            return Json(model);
        }

        public async Task<IActionResult> WeeklyAnalysisListAsync(PredictionSearchModel searchModel)
        {
            var model = await _salesForecastingModelFactory.PrepareWeeklyPredictionList(searchModel);
            return Json(model);
        }

        public IActionResult Analysis(TestFeatureModel testFeatureModel)
        {
            // Create a list to hold the dummy objects
            List<object> dummyObjects = new List<object>();

            // Add 10 dummy objects to the list
            for (int i = 0; i < 10; i++)
            {
                var dummyObject = new
                {
                    WeekName = $"Week {i + 1}",
                    WeeklySalesQuantity = 10 * (i + 1)
                };

                dummyObjects.Add(dummyObject);
            }

            return Json(dummyObjects);
        }

        #endregion
    }
}