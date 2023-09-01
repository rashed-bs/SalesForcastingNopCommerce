using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Stores;
using Nop.Data;
using NopStation.Plugin.Misc.SalesForecasting.Services;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Stores;
using Nop.Web.Framework.Models.Extensions;
using NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Core.Domain.Catalog;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Services.Media;
using NopStation.Plugin.Misc.SalesForecasting.Domain;
using Nop.Web.Framework.Models.Extensions;
using System;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Factories
{
    public class SalesForecastingModelFactory : ISalesForecastingModelFactory
    {
        private readonly ICategoryService _categoryService;
        private readonly ILocalizationService _localizationService;
        private readonly ISalesForecastingService _mLModelService;
        private readonly IProductService _productService;
        private readonly IPictureService _pictureService;
        private readonly IRepository<GroupRelatedProduct> _groupRelatedProductRepository;
        private readonly IRepository<ProductGroup> _productGroupRepository;
        protected readonly IRepository<Product> _productRepository;
        public SalesForecastingModelFactory(ILocalizationService localizationService,
            ICategoryService categoryService,
            ISalesForecastingService mLModelService,
            IProductService productService,
            IRepository<GroupRelatedProduct> groupRelatedProductRepository,
            IRepository<Product> productRepository,
            IPictureService pictureService,
            IRepository<ProductGroup> productGroupRepository)
        {
            _categoryService = categoryService;
            _localizationService = localizationService;
            _mLModelService = mLModelService;
            _productService = productService;
            _pictureService = pictureService;
            _productRepository = productRepository;
            _groupRelatedProductRepository = groupRelatedProductRepository;
            _productGroupRepository = productGroupRepository;
        }

        public async Task<PredictionSearchModel> PreparePredictionSearchModel(PredictionSearchModel predictionSearchModel, string defaultValue = "0")
        {
            var categories = await _categoryService.GetAllCategoriesAsync();

            var defaultItemText = await _localizationService.GetResourceAsync("Admin.Common.All");
            predictionSearchModel.AvailableCategory.Insert(0, new SelectListItem { Value = defaultValue, Text = defaultItemText, Selected = true });
            foreach (var categoryItem in categories)
                predictionSearchModel.AvailableCategory.Add(new SelectListItem
                {
                    Value = categoryItem.Id.ToString(),
                    Text = categoryItem.Name,
                });
            predictionSearchModel.SetGridPageSize();
            return predictionSearchModel;
        }

        public virtual async Task<ProductGroupModel> PrepareProductGroupModelAsync(ProductGroupModel productGroupModel, ProductGroup productGroup)
        {
            if (productGroupModel == null && productGroup == null)
            {
                return new ProductGroupModel();
            }
            if (productGroupModel == null)
            {
                var model = new ProductGroupModel();
                model.Id = productGroup.Id;
                model.IsActive = productGroup.IsActive;
                model.GroupName = productGroup.GroupName;
                model.DiscountAppliedFrequently = productGroup.DiscountAppliedFrequently;
                model.RelatedProductGroupSearchModel = new RelatedProductGroupSearchModel();
                model.RelatedProductGroupSearchModel.ProductGroupId = productGroup.Id;
                model.GroupProductPredictionSearchModel = new GroupProductPredictionSearchModel();
                model.GroupProductPredictionSearchModel.ProductGroupId = productGroup.Id;
                return model;
            }
            return new ProductGroupModel();
        }

        public async Task<IEnumerable<ProductModel>> PrepareProductModelsAsync(IEnumerable<Product> products)
        {
            return await products.SelectAwait(async product =>
            {
                //fill in model values from the entity
                var productModel = product.ToModel<ProductModel>();

                //little performance optimization: ensure that "FullDescription" is not returned
                productModel.FullDescription = string.Empty;
                var defaultProductPicture = (await _pictureService.GetPicturesByProductIdAsync(product.Id, 1)).FirstOrDefault();
                (productModel.PictureThumbnailUrl, _) = await _pictureService.GetPictureUrlAsync(defaultProductPicture, 75);
                return productModel;
            }).ToListAsync();
        }


        public virtual async Task<RelatedProductGroupListModel> PrepareRelatedProductListModelAsync(RelatedProductGroupSearchModel searchModel, ProductGroup productGroup)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            if (productGroup == null)
                throw new ArgumentNullException(nameof(productGroup));

            var query = from rp in _groupRelatedProductRepository.Table
                        where rp.ProductId1 == productGroup.Id
                        select rp;
            var relatedProducts = (await query.ToListAsync()).ToPagedList(searchModel);

            //prepare grid model
            var model = await new RelatedProductGroupListModel().PrepareToGridAsync(searchModel, relatedProducts, () =>
            {
                return relatedProducts.SelectAwait(async relatedProduct =>
                {
                    var relatedProductGroupModel = new RelatedProductGroupModel()
                    {
                        Id = relatedProduct.Id,
                        ProductId2 = relatedProduct.ProductId2,
                        DisplayOrder = relatedProduct.DisplayOrder,
                    };

                    //fill in additional values (not existing in the entity)
                    relatedProductGroupModel.Product2Name = (await _productService.GetProductByIdAsync(relatedProduct.ProductId2))?.Name;

                    return relatedProductGroupModel;
                });
            });
            return model;
        }

        public GroupProductSearchModel PrepareProductGroupSearchModelAsync(GroupProductSearchModel groupProductSearchModel)
        {
            return new GroupProductSearchModel();
        }
        
        public async Task<ProductGroupListModel> PrepareProductGroupListModelAsync(GroupProductSearchModel groupProductSearchModel)
        {
            var query = from p in _productGroupRepository.Table
                        where (groupProductSearchModel.GroupName == null || (p.GroupName.ToLower().StartsWith(groupProductSearchModel.GroupName.ToLower())))
                        select p;
            var productGroupList = (await query.ToListAsync()).ToPagedList(groupProductSearchModel);
            // prepare grid model 
            var model = await new ProductGroupListModel().PrepareToGridAsync(groupProductSearchModel, productGroupList, () =>
            {
                return productGroupList.SelectAwait(async productGroup =>
                {
                    var productGroupModel = new ProductGroupModel()
                    {
                        Id = productGroup.Id,
                        GroupName = productGroup.GroupName,
                        DiscountAppliedFrequently = productGroup.DiscountAppliedFrequently,
                        IsActive = productGroup.IsActive,
                    };
                    return productGroupModel;
                });
            });
            return model;
        }
    }
}

