using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using System.Reflection;

namespace Meditrans.Client.Models.Csv
{


    public sealed class CsvTripRawModelMap : ClassMap<CsvTripRawModel>
    {
        public CsvTripRawModelMap(
            Dictionary<string, int> headerIndices,
            Dictionary<string, string> csvToPropertyMappings) // Este ya debería ser case-insensitive
        {
            foreach (var mappingEntry in csvToPropertyMappings)
            {
                string csvHeaderName = mappingEntry.Key;
                string propertyName = mappingEntry.Value;

                if (headerIndices.TryGetValue(csvHeaderName, out int actualColumnIndex)) // headerIndices ya es case-insensitive
                {
                    var propertyInfo = typeof(CsvTripRawModel).GetProperty(propertyName);
                    if (propertyInfo != null)
                    {
                        Map(typeof(CsvTripRawModel), propertyInfo).Index(actualColumnIndex).Name(csvHeaderName); // Añadir .Name() puede ayudar a CsvHelper
                    }
                    else
                    {
                        Console.WriteLine($"Advertencia: La propiedad '{propertyName}' no fue encontrada en CsvTripRawModel para el encabezado CSV '{csvHeaderName}'.");
                    }
                }
                else
                {
                    Console.WriteLine($"Advertencia: El encabezado CSV '{csvHeaderName}' (definido en el mapeo JSON) no fue encontrado en el archivo CSV leído.");
                }
            }
        }
    }
}
