using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NopStation.Plugin.Misc.SalesForecasting.Models
{
    public class CategoryAvgPrice
    {
        public int CategoryId { get; set; }
        public float AvgPrice { get; set; }
    }
    
    public class TempCategoryAvgPrice
    {
        public int CategoryId { get; set; }
        public float OrderTotal { get; set; }
        public int Unit { get; set; }
    }
}
