using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ML.Data;

namespace NopStation.Plugin.Misc.SalesForecasting.Areas.Admin.Models
{
    public class InventoryPredictionModel
    {
        [ColumnName("Score")]
        public float SalesQuantity { get; set; }
    }
}
