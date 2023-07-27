using Microsoft.ML.Data;

namespace NopStation.Plugin.Misc.SalesForecasting.Models
{
    public class SalesData
    {
        // The index of column in LoadColumn(int index) should be matched with the position of columns in the underlying data file.
        // The next column is used by the Regression algorithm as the Label (e.g. the value that is being predicted by the Regression model).
        [LoadColumn(0)]
        public float Next { get; set; }

        [LoadColumn(1)]
        public float Year { get; set; }

        [LoadColumn(2)]
        public float Month { get; set; }

        [LoadColumn(3)]
        public float Units { get; set; }

        [LoadColumn(4)]
        public float Avg { get; set; }

        [LoadColumn(5)]
        public float Count { get; set; }

        [LoadColumn(6)]
        public float Max { get; set; }

        [LoadColumn(7)]
        public float Min { get; set; }

        [LoadColumn(8)]
        public float Prev { get; set; }

        public override string ToString()
        {
            return $"SalesData [ year: {Year}, month: {Month:00}, next: {Next:0000}, units: {Units:0000}, avg: {Avg:000}, count: {Count:00}, max: {Max:000}, min: {Min}, prev: {Prev:0000} ]";
        }
    }
}
