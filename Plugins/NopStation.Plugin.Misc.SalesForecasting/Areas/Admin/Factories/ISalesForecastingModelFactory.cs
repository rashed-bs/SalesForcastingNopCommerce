using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Web.Areas.Admin.Models.Catalog;
using NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models;
using NopStation.Plugin.Misc.SalesForecasting.Domain;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Factories
{
    public interface ISalesForecastingModelFactory
    {
        Task<PredictionSearchModel> PreparePredictionSearchModel(PredictionSearchModel predictionSearchModel, string defaultValue = "0");
        Task<IEnumerable<ProductModel>> PrepareProductModelsAsync(IEnumerable<Product> products);
        Task<ProductGroupModel> PrepareProductGroupModelAsync(ProductGroupModel productGroupModel, ProductGroup productGroup);

        Task<RelatedProductGroupListModel> PrepareRelatedProductListModelAsync(RelatedProductGroupSearchModel searchModel, ProductGroup productGroup);

        Task<ProductGroupListModel> PrepareProductGroupListModelAsync(GroupProductSearchModel groupProductSearchModel);

        GroupProductSearchModel PrepareProductGroupSearchModelAsync(GroupProductSearchModel groupProductSearchModel);
    }
}
