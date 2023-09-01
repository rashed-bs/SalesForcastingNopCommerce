using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Vendors;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using NopStation.Plugin.Misc.Core.Controllers;
using NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Factories;
using NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models;
using NopStation.Plugin.Misc.SalesForecasting.Domain;
using NopStation.Plugin.Misc.SalesForecasting.Services;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Controllers
{
    public class ProductGroupController : NopStationAdminController
    {
        #region Fields
        private readonly IRepository<ProductGroup> _productGroupRepository;
        private readonly IRepository<GroupRelatedProduct> _groupRelatedProductRepository;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly ISalesForecastingService _salesForecastingService;
        private readonly IProductService _productService;
        private readonly ISalesForecastingModelFactory _salesForecastingModelFactory;
        private readonly IProductModelFactory _productModelFactory;
        private readonly IWorkContext _workContext;
        #endregion

        #region Ctor

        public ProductGroupController(ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext,
            ISalesForecastingService salesForecastingService,
            IProductService productService,
            ISalesForecastingModelFactory salesForecastingModelFactory,
            IProductModelFactory productModelFactory,
            IWorkContext workContext,
            IRepository<GroupRelatedProduct> groupRelatedProductRepository,
            IRepository<ProductGroup> productGroupRepository)
        {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _storeContext = storeContext;
            _salesForecastingService = salesForecastingService;
            _productModelFactory = productModelFactory;
            _productService = productService;
            _salesForecastingModelFactory = salesForecastingModelFactory;
            _productGroupRepository = productGroupRepository;
            _workContext = workContext;
            _groupRelatedProductRepository = groupRelatedProductRepository;
        }

        #endregion

        #region Methods 

        #region List
        public virtual IActionResult Index()
        {
            return RedirectToAction("List");
        }

        public virtual async Task<IActionResult> List()
        {
            //prepare model
            var model =  _salesForecastingModelFactory.PrepareProductGroupSearchModelAsync(new GroupProductSearchModel());

            return View(model);
        }

        public virtual async Task<IActionResult> ProductGroupList(GroupProductSearchModel searchModel)
        {
            var model = await _salesForecastingModelFactory.PrepareProductGroupListModelAsync(searchModel);
            return Json(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> DeleteProductGroupsSelected(ICollection<int> selectedIds)
        {
            if (selectedIds == null || selectedIds.Count == 0)
                return NoContent();

            await _productGroupRepository.DeleteAsync((await _productGroupRepository.GetByIdsAsync(selectedIds.ToArray())));
            return Json(new { Result = true });
        }

        
        #endregion

        #region Create / Edit / Delete
        public virtual async Task<IActionResult> Create(bool showtour = false)
        {
            //prepare model
            var model = await _salesForecastingModelFactory.PrepareProductGroupModelAsync(new ProductGroupModel(), null);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public virtual async Task<IActionResult> Create(ProductGroupModel model, bool continueEditing)
        {
            
            if (ModelState.IsValid)
            {
                //productGroup
                var productGroup = new ProductGroup()
                {
                    IsActive = model.IsActive,
                    DiscountAppliedFrequently = model.DiscountAppliedFrequently,
                    GroupName = model.GroupName,
                };
                productGroup.CreatedOnUtc = DateTime.UtcNow;
                productGroup.UpdatedOnUtc = DateTime.UtcNow;
                await _productGroupRepository.InsertAsync(productGroup);

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Catalog.Products.Added"));

                if (!continueEditing)
                    return RedirectToAction("List");

                return RedirectToAction("Edit", new { id = productGroup.Id });
            }

            //prepare model
            model = await _salesForecastingModelFactory.PrepareProductGroupModelAsync(model, null);

            //if we got this far, something failed, redisplay form
            return View(model);
        }



        public virtual async Task<IActionResult> Edit(int id)
        {
            var productGroup = await _productGroupRepository.GetByIdAsync(id);
            if (productGroup == null)
                return RedirectToAction("List");

            //prepare model
            var model = await _salesForecastingModelFactory.PrepareProductGroupModelAsync(null, productGroup);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public virtual async Task<IActionResult> Edit(ProductGroupModel productGroupModel, bool continueEditing)
        {
            //try to get a product with the specified id
            var productGroup = await _productGroupRepository.GetByIdAsync(productGroupModel.Id);
            if (productGroup == null)
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                productGroup.IsActive = productGroupModel.IsActive;
                productGroup.GroupName = productGroupModel.GroupName;
                productGroup.DiscountAppliedFrequently = productGroupModel.DiscountAppliedFrequently;
                productGroup.UpdatedOnUtc = DateTime.UtcNow;
                await _productGroupRepository.UpdateAsync(productGroup);

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Catalog.Products.Updated"));

                if (!continueEditing)
                    return RedirectToAction("List");

                return RedirectToAction("Edit", new { id = productGroup.Id });
            }

            //prepare model
            productGroupModel = await _salesForecastingModelFactory.PrepareProductGroupModelAsync(null, productGroup);

            //if we got this far, something failed, redisplay form
            return View(productGroupModel);
        }
        #endregion

        #region Related products

        [HttpPost]
        public virtual async Task<IActionResult> RelatedProductList(RelatedProductGroupSearchModel searchModel)
        {
            //try to get a product with the specified id
            var productGroup = await _productGroupRepository.GetByIdAsync(searchModel.ProductGroupId)
                ?? throw new ArgumentException("No product found with the specified id");

            //prepare model
            var model = await _salesForecastingModelFactory.PrepareRelatedProductListModelAsync(searchModel, productGroup);

            return Json(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> RelatedProductUpdate(RelatedProductGroupModel model)
        {
            //try to get a related product with the specified id
            var relatedProduct = await _groupRelatedProductRepository.GetByIdAsync(model.Id)
                ?? throw new ArgumentException("No related product found with the specified id");

            relatedProduct.DisplayOrder = model.DisplayOrder;
            await _groupRelatedProductRepository.UpdateAsync(relatedProduct);
            return new NullJsonResult();
        }

        [HttpPost]
        public virtual async Task<IActionResult> RelatedProductDelete(int id)
        {
            //try to get a related product with the specified id
            var relatedProduct = await _groupRelatedProductRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("No related product found with the specified id");

            await _groupRelatedProductRepository.DeleteAsync(relatedProduct);

            return new NullJsonResult();
        }

        public virtual async Task<IActionResult> RelatedProductAddPopup(int productId)
        {
            //prepare model
            var model = await _productModelFactory.PrepareAddRelatedProductSearchModelAsync(new AddRelatedProductSearchModel());

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> RelatedProductAddPopupList(AddRelatedProductSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageProducts))
                return await AccessDeniedDataTablesJson();

            //prepare model
            var model = await _productModelFactory.PrepareAddRelatedProductListModelAsync(searchModel);

            return Json(model);
        }

        [HttpPost]
        [FormValueRequired("save")]
        public virtual async Task<IActionResult> RelatedProductAddPopup(AddRelatedProductGroupModel model)
        {
            var selectedProducts = await _productService.GetProductsByIdsAsync(model.SelectedProductIds.ToArray());
            
            if (selectedProducts.Any())
            {
                var query = from p in _groupRelatedProductRepository.Table
                            where p.ProductId1 == model.ProductGroupId
                            select p.ProductId2;
                var existingProducts = await query.ToListAsync();
                foreach(var product in selectedProducts)
                {
                    if (existingProducts.Contains(product.Id))
                        continue;
                    else
                    {
                        await _groupRelatedProductRepository.InsertAsync(new GroupRelatedProduct()
                        {
                            ProductId1 = model.ProductGroupId,
                            ProductId2 = product.Id,
                            DisplayOrder = product.DisplayOrder,
                        });
                    }
                }
            }

            ViewBag.RefreshPage = true;

            return View(new AddRelatedProductSearchModel());
        }

        #endregion

        #region Train / Forcasting











        #endregion

        #endregion
    }
}
