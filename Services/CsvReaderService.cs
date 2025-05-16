
using System.Globalization;
using System.IO;
using Meditrans.Client.Models.Csv;
using CsvHelper;
using CsvHelper.Configuration;


namespace Meditrans.Client.Services
{
    public class CsvReaderService
    {
        private readonly string _csvFilePath;
        private readonly string _jsonFileName;
        public CsvReaderService(string csvFilePath, string jsonFileName)
        {
            _csvFilePath = csvFilePath;
            _jsonFileName = jsonFileName;
        }
        public List<CsvTripRawModel> ReadCsv()
        {
            using var reader = new StreamReader(_csvFilePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            });

            return csv.GetRecords<CsvTripRawModel>().ToList();
        }

        // If ReadCsv may take a long time, consider async Task<List<CsvTripRawModel>>
        // and run it with Task.Run().
        public List<CsvTripRawModel> ReadCsvWithoutDuplicateColumns()
        {

            // Define CsvHelper settings
            var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                HeaderValidated = null,
                MissingFieldFound = (args) =>
                {
                    Console.WriteLine($"Missing field found. Headers: '{string.Join(", ", args.HeaderNames ?? new string[0])}', Index: {args.Index}, Row: {args.Context.Parser.Row}");
                },
            };

            using var reader = new StreamReader(_csvFilePath);
            using var csv = new CsvHelper.CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader();
            var actualCsvHeaders = csv.Context.Reader.HeaderRecord;
            if (actualCsvHeaders == null)
            {
                throw new InvalidOperationException("The CSV file contains no headers or is empty.");
            }

            // Remove duplicate columns
            var uniqueHeaderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var headerOriginalIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < actualCsvHeaders.Length; i++)
            {
                var header = actualCsvHeaders[i];
                if (!uniqueHeaderNames.Contains(header))
                {
                    uniqueHeaderNames.Add(header);
                    headerOriginalIndices[header] = i;
                }
            }

            //string mappingFileName = "SAFERIDE.json";
            string mappingFileName = _jsonFileName;
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string mappingPath = System.IO.Path.Combine(baseDirectory, "Assets", "Mappings", mappingFileName);

            if (!File.Exists(mappingPath))
            {
                throw new FileNotFoundException($"The mapping file was not found: {mappingPath}");
            }

            var mappingJson = File.ReadAllText(mappingPath);
            var tempMapping = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(mappingJson);

            if (tempMapping == null)
            {
                throw new Exception($"Error deserializing JSON mapping file: {mappingFileName}.");
            }
            var csvToPropertyMappings = new Dictionary<string, string>(tempMapping, StringComparer.OrdinalIgnoreCase);

            var map = new CsvTripRawModelMap(headerOriginalIndices, csvToPropertyMappings);
            csv.Context.RegisterClassMap(map);

            return csv.GetRecords<CsvTripRawModel>().ToList();
        }
       
    }

}
