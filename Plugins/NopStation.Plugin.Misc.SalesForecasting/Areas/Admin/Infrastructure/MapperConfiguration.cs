using AutoMapper;
using Nop.Core.Infrastructure.Mapper;
using NopStation.Plugin.Misc.Core.Models;
using NopStation.Plugin.Misc.SalesForecasting;

namespace NopStation.Plugin.Misc.PowerBI.Areas.Admin.Infrastructure
{
    public class MapperConfiguration : Profile, IOrderedMapperProfile
    {
        #region Ctor

        public MapperConfiguration()
        {
            CreateMap<SalesForecastingSettings, ConfigurationModel>();
            CreateMap<ConfigurationModel, SalesForecastingSettings>();
        }

        #endregion

        #region Properties

        public int Order => 0;

        #endregion
    }
}