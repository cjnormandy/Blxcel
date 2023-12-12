using Microsoft.AspNetCore.Components.Forms;
using OfficeOpenXml;

namespace BlazeApp.Data;
public class FileUploadService
{
    public async Task<List<List<string>>?> ProcessFileAsync(InputFileChangeEventArgs e)
    {
        var res = new List<List<string>>();
        var file = e.File;
        // Ensure file is not null
        if (file != null && Path.GetExtension(file.Name).Equals(".xlsx"))
        {
            using (var stream = new MemoryStream())
            {
                await file.OpenReadStream().CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;
                    int colCount = worksheet.Dimension.Columns;

                    for (int row = 1; row <= rowCount; row++)
                    {
                        var rowData = new List<string>();
                        for (int col = 1; col <= colCount; col++)
                        {
                            var value = worksheet.Cells[row, col].Value?.ToString() ?? string.Empty;
                            rowData.Add(value);
                        }
                        res.Add(rowData);
                        Console.WriteLine($"This is the data from res: {res}");
                    }
                }
            }
            return res;
        }
        return null;
    }
}