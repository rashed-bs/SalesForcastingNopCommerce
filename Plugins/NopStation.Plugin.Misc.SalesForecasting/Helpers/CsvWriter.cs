using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Drawing.Diagrams;

namespace NopStation.Plugin.Misc.SalesForecasting.Helpers
{
    public static class CsvWriter
    {
        public static void WriteToCsv<T>(string filePath, IEnumerable<T> data)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (data == null)
                throw new ArgumentNullException(nameof(data), "Data cannot be null.");

            var type = typeof(T);
            var properties = type.GetProperties();

            if (properties.Length == 0)
                throw new InvalidOperationException($"Type '{type.Name}' does not have any public properties to write to the CSV.");

            using (var writer = new StreamWriter(filePath))
            {
                var headerRow = string.Empty;
                // Write header row with property names
                bool addComma = false;
                foreach (var p in properties)
                {
                    if (addComma)
                        headerRow += ",";
                    headerRow += p.Name;
                    addComma = true;
                }
                writer.WriteLine(headerRow);

                // Write data rows
                foreach (var item in data)
                {
                    var values = new List<string>();
                    foreach (var property in properties)
                    {
                        var value = property.GetValue(item);
                        values.Add(FormatValue(value));
                    }

                    var dataRow = string.Join(",", values);
                    writer.WriteLine(dataRow);
                }
            }
        }
        private static string FormatValue(object value)
        {
            if (value == null)
                return string.Empty;

            // Handle special formatting for specific data types if needed
            if (value is DateTime dateTimeValue)
                return dateTimeValue.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            // Add more special formatting cases here if necessary...

            // Use ToString() for other types
            return value.ToString();
        }
    }



}
