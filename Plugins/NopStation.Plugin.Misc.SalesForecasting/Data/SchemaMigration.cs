using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using NopStation.Plugin.Misc.SalesForecasting.Domain;

namespace NopStation.Plugin.Misc.SalesForecasting.Data
{
    [NopMigration("2023/09/04 11:00:00:6455421", "Misc.Sales Forcasting Migration", MigrationProcessType.Installation)]
    public class SchemaMigration : AutoReversingMigration
    {
        #region Methods

        /// <summary>
        /// Collect the UP migration expressions
        /// </summary>
        public override void Up()
        {
            Create.TableFor<ProductGroup>();
            Create.TableFor<GroupRelatedProduct>();
            Create.TableFor<GroupProductsPrediction>();
        }

        #endregion
    }
}
