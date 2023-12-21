using System.Data;
using Microsoft.AspNetCore.Components;
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

        // MARK: DataTable Logic
        private DataTable cityDetailTable = CreateCityDetailTable();
        private List<CityInfo> cityInfoList = new List<CityInfo>();


        // MARK: Edit table logic
        string EditRowId = null;
        int? selectedRow1 = null;
        int? selectedRow2 = null;
        private int? currentlySelectedRow = null;



        void BeginEdit(DataRow row)
        {
            EditRowId = row["cityDetailId"].ToString();
        }

        void EndEdit()
        {
            EditRowId = null;
        }

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
            cityDetailTable.Clear();

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

            ConvertDictionaryToModel(result);

            return result;
        }

        public List<CityInfo> ConvertDictionaryToModel(List<Dictionary<string, object>> data)
        {
            var models = new List<CityInfo>();
            foreach (var row in data)
            {
                var model = new CityInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = row.ContainsKey("Name") ? Convert.ToString(row["Name"]) : null,
                    Population = row.ContainsKey("Population") ? Convert.ToInt64(row["Population"]) : 0,
                    Country = row.ContainsKey("Country") ? Convert.ToString(row["Country"]) : null,
                    Area = row.ContainsKey("Area") ? Convert.ToDouble(row["Area"]) : 0,
                    Seen = row.ContainsKey("Seen") ? Convert.ToString(row["Seen"]) : "N",
                };

                models.Add(model);
            }

            cityInfoList = models;
            InsertCityDetails(cityDetailTable, models);
            
            return models;
        }

        public static void InsertCityDetails(DataTable cityDT, List<CityInfo> cityRows)
        {
            foreach(CityInfo cityInfo in cityRows)
            {
                DataRow row = cityDT.NewRow();

                row["cityDetailId"] = cityInfo.Id;
                row["cityName"] = cityInfo.Name;
                row["cityPopulation"] = cityInfo.Population;
                row["cityCountry"] = cityInfo.Country;
                row["cityArea"] = cityInfo.Area;
                row["citySeen"] = cityInfo.Seen;

                cityDT.Rows.Add(row);
            }
        }

        private static DataTable CreateCityDetailTable()
        {
            DataTable cityDetailDT = new DataTable("cityDetail");
            DataColumn[] cols =
            {
                new DataColumn("cityDetailId", typeof(string)),
                new DataColumn("cityName", typeof(string)),
                new DataColumn("cityPopulation", typeof(long)),
                new DataColumn("cityCountry", typeof(string)),
                new DataColumn("cityArea", typeof(double)),
                new DataColumn("citySeen", typeof(string)),
            };

            cityDetailDT.Columns.AddRange(cols);

            return cityDetailDT;      
        }

        private void OnInputChange(ChangeEventArgs e, string cityInfoId, string columnName)
        {
            var updatedVal = e.Value.ToString();
            var cityInfo = cityInfoList.FirstOrDefault(c => c.Id == cityInfoId);

            if (cityInfo != null)
            {
                var row = cityDetailTable.AsEnumerable().FirstOrDefault(r => r.Field<string>("cityDetailId") == cityInfoId);
                if (row != null)
                {
                    row[columnName] = updatedVal;
                }

                UpdateCityInfoModel(cityInfo, columnName, updatedVal);
            }
        }

        private void OnCheckboxChange(ChangeEventArgs e, DataRow row, string columnName)
        {
            bool isChecked = (bool)e.Value;
            string newValue = isChecked ? "Y" : "N";
            row[columnName] = newValue;

            // Find and update the corresponding CityInfo object
            var cityInfoId = row["cityDetailId"].ToString();
            var cityInfo = cityInfoList.FirstOrDefault(c => c.Id == cityInfoId);
            if (cityInfo != null)
            {
                UpdateCityInfoModel(cityInfo, columnName, newValue);
            }
        }

        private void UpdateCityInfoModel(CityInfo cityInfo, string columnName, string updatedValue)
        {
            switch (columnName)
            {
                case "cityName":
                    cityInfo.Name = updatedValue;
                    break;
                case "cityPopulation":
                    cityInfo.Population = Convert.ToInt64(updatedValue);
                    break;
                case "cityCountry":
                    cityInfo.Country = updatedValue;
                    break;
                case "cityArea":
                    cityInfo.Area = Convert.ToDouble(updatedValue);
                    break;
                case "citySeen":
                    cityInfo.Seen = updatedValue;
                    break;
            }
        }



        private bool IsLastRow(DataRow row)
        {
            return cityDetailTable.Rows.IndexOf(row) == cityDetailTable.Rows.Count - 1;
        }

        private void DeleteCity(string cityInfoId)
        {
            foreach (DataRow row in cityDetailTable.Rows)
            {
                if (row["cityDetailId"].ToString() == cityInfoId)
                {
                    cityDetailTable.Rows.Remove(row);
                    break;
                }
            }
        }

        private void DeleteCityAndRefreshUI(string cityInfoId)
        {
            DeleteCity(cityInfoId);
            StateHasChanged();
        }

        void SelectRowForSwap(int rowIndex)
        {
            currentlySelectedRow = rowIndex;

            if (selectedRow1 == null)
            {
                selectedRow1 = rowIndex;
            }
            else
            {
                selectedRow2 = rowIndex;

                if (selectedRow1.HasValue && selectedRow2.HasValue)
                {
                    SwapRows(selectedRow1.Value, selectedRow2.Value);
                    selectedRow1 = null;
                    selectedRow2 = null;
                }
            }
            StateHasChanged();
        }

        void SwapRows(int idx1, int idx2)
        {
            var temp = cityDetailTable.Rows[idx1].ItemArray;
            cityDetailTable.Rows[idx1].ItemArray = cityDetailTable.Rows[idx2].ItemArray;
            cityDetailTable.Rows[idx2].ItemArray = temp;
        }

        void SortRowsBy(string colName)
        {
            // Store the last row's data before removing it from the table
            DataRow lastRow = cityDetailTable.Rows[cityDetailTable.Rows.Count - 1];
            object[] lastRowData = lastRow.ItemArray;

            // Remove the last row from the table
            cityDetailTable.Rows.RemoveAt(cityDetailTable.Rows.Count - 1);

            // Sort the remaining rows
            DataView view = cityDetailTable.DefaultView;
            view.Sort = colName;
            cityDetailTable = view.ToTable();

            // Create a new row with the copied data and add it to the table
            DataRow newRow = cityDetailTable.NewRow();
            newRow.ItemArray = lastRowData;
            cityDetailTable.Rows.Add(newRow);
        }


    }
}