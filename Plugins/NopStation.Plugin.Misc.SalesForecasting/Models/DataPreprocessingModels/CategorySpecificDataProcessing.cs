using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NopStation.Plugin.Misc.SalesForecasting.Models.DataPreprocessingModels
{
    public class CategoryAvgPrice
    {
        public int CategoryId { get; set; }
        public float AvgPrice { get; set; }
    }

    public class CategoryTotalSelling
    {
        public int CategoryId { get; set; }
        public float CumulativeOrderTotal { get; set; }
    }

    public class CategoryTotalTempSelling
    {
        public DateTime OrderDate { get; set; }
        public int CategoryId { get; set; }
        public float OrderTotal { get; set; }
    }

    public class TempCategoryAvgPrice
    {
        public int CategoryId { get; set; }
        public float OrderTotal { get; set; }
        public int Unit { get; set; }
    }

    public class CategoryMappingSubToRoot
    {
        public int SubCategoryId { get; set; }
        public int RootId { get; set; }
    }
}
