using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Services.Security;
using System.Collections.Generic;

namespace NopStation.Plugin.Misc.SalesForecasting.Extensions
{
    public class SalesForecastingProvider : IPermissionProvider
    {
        public static readonly PermissionRecord ManageConfiguration = new PermissionRecord { Name = "NopStation sales forecasting. Configuration", SystemName = "ManageSalesForecastingConfiguration", Category = "NopStation" };
        public static readonly PermissionRecord ViewSalesForecasting = new PermissionRecord { Name = "NopStation sales forecasting. View sales forecasting", SystemName = "ViewSalesForecasting", Category = "NopStation" };

        public virtual HashSet<(string systemRoleName, PermissionRecord[] permissions)> GetDefaultPermissions()
        {
            return new HashSet<(string, PermissionRecord[])>
            {
                (
                    NopCustomerDefaults.AdministratorsRoleName,
                    new[]
                    {
                            ManageConfiguration,
                            ViewSalesForecasting
                    }
                )
            };
        }

        public IEnumerable<PermissionRecord> GetPermissions()
        {
            return new[]
            {
                ManageConfiguration,
                ViewSalesForecasting
            };
        }       
    }
}
