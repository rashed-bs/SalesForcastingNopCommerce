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

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Factories
{
    public class SalesForecastingModelFactory : ISalesForecastingModelFactory
    {
        private readonly ICategoryService _categoryService;
        private readonly ILocalizationService _localizationService;
        private readonly ISalesForecastingService _mLModelService;
        private readonly IProductService _productService;
        private readonly IPictureService _pictureService;
        public SalesForecastingModelFactory(ILocalizationService localizationService,
            ICategoryService categoryService,
            ISalesForecastingService mLModelService,
            IProductService productService,
            IPictureService pictureService)
        {
            _categoryService = categoryService;
            _localizationService = localizationService;
            _mLModelService = mLModelService;
            _productService = productService;
            _pictureService = pictureService;
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

        public virtual async Task<ProductGroupModel> PrepareProductGroupModelAsync(ProductGroupModel productGroupModel)
        {
            if(productGroupModel == null)
            {
                var model = new ProductGroupModel();
                return model;
            }
            return productGroupModel;
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
    }
}

