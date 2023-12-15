using Microsoft.AspNetCore.Components.Forms;
using OfficeOpenXml;

namespace BlazeApp.Pages
{
    public partial class FileUpload1
    {
        private List<IBrowserFile> loadedFiles = new();
        private List<string> columnHeaders = new();
        private List<List<object>> genRes = new();
        private List<Dictionary<string, object>> result = new();
        private long maxFileSize = 1024 * 15;
        private int maxAllowedFiles = 3;
        private bool isLoading;

        private async Task ReadExcelDataAsync(InputFileChangeEventArgs e)
        {
            isLoading = true;
            genRes.Clear();
            loadedFiles.Clear();

            foreach (var file in e.GetMultipleFiles())
            {
                using var memoryStream = new MemoryStream();
                await file.OpenReadStream().CopyToAsync(memoryStream);

                memoryStream.Position = 0;
                ReadExcelToDictionaryAsync(memoryStream);
                try
                {
                    using (ExcelPackage package = new ExcelPackage(memoryStream))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                        int rowCount = 7;
                        int colCount = 4;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            List<object> rowData = new List<object>();
                            for (int col = 1; col <= colCount; col++)
                            {
                                object value = worksheet.Cells[row, col].Value;
                                rowData.Add(value);
                            }
                            genRes.Add(rowData);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error processing file: " + ex.Message);
                }
            }

            isLoading = false;
            
        }

        public List<Dictionary<string, object>> ReadExcelToDictionaryAsync(Stream fileStream)
        {
            result.Clear();
            columnHeaders.Clear();

            using (var package = new ExcelPackage(fileStream))
            {
                var worksheet = package.Workbook.Worksheets[0];

                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    columnHeaders.Add(worksheet.Cells[1, col].Text);
                }

                for (int row = 2; row <= 7; row++)
                {
                    var rowDict = new Dictionary<string, object>();
                    for (int col = 1; col <= columnHeaders.Count; col++)
                    {
                        var key = columnHeaders[col - 1];
                        var value = worksheet.Cells[row, col].Value;
                        rowDict.Add(key, value);
                    }
                    result.Add(rowDict);
                }
            }
            return result;
        }

        public List<CityInfo> ConvertDictionaryToModel(List<Dictionary<string, object>> data)
        {
            var models = new List<CityInfo>();
            foreach (var row in data)
            {
                var model = new CityInfo
                {
                    Name = row.ContainsKey("Name") ? Convert.ToString(row["Name"]) : null,
                    Population = row.ContainsKey("Population") ? Convert.ToInt64(row["Population"]) : 0,
                    Country = row.ContainsKey("Country") ? Convert.ToString(row["Country"]) : null,
                    Area = row.ContainsKey("Area") ? Convert.ToDouble(row["Area"]) : 0,
                };

                models.Add(model);
            }
            return models;
        }
    }
}