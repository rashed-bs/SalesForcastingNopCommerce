using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Vendors;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Framework.Mvc.Filters;
using NopStation.Plugin.Misc.Core.Controllers;
using NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Factories;
using NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models;
using NopStation.Plugin.Misc.SalesForecasting.Services;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Controllers
{
    public class ProductGroupController : NopStationAdminController
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

        public ProductGroupController(ILocalizationService localizationService,
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
        public virtual async Task<IActionResult> Create(bool showtour = false)
        {
            //prepare model
            var model = await _salesForecastingModelFactory.PrepareProductGroupModelAsync(new ProductGroupModel());

            return View(model);
        }
        /*

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public virtual async Task<IActionResult> Create(ProductGroupModel model, bool continueEditing)
        {
            
            if (ModelState.IsValid)
            {
                //productGroup
                var product = model.ToEntity<>();
                product.CreatedOnUtc = DateTime.UtcNow;
                product.UpdatedOnUtc = DateTime.UtcNow;
                await _productService.InsertProductAsync(product);

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Catalog.Products.Added"));

                if (!continueEditing)
                    return RedirectToAction("List");

                return RedirectToAction("Edit", new { id = product.Id });
            }

            //prepare model
            model = await _salesForecastingModelFactory.PrepareProductGroupModelAsync(model);

            //if we got this far, something failed, redisplay form
            return View(model);
        }
        */

        #endregion
    }
}
