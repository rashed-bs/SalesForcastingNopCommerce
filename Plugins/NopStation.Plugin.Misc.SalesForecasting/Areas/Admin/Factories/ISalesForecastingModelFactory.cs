using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Web.Areas.Admin.Models.Catalog;
using NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Factories
{
    public interface ISalesForecastingModelFactory
    {
        Task<PredictionSearchModel> PreparePredictionSearchModel(PredictionSearchModel predictionSearchModel, string defaultValue = "0");
        Task<IEnumerable<ProductModel>> PrepareProductModelsAsync(IEnumerable<Product> products);
    }
}
