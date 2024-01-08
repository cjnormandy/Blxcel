using System.Data;
using System.Drawing.Design;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR;
using OfficeOpenXml;
using OfficeOpenXml.Export.HtmlExport.Accessibility;
using static System.Text.Json.JsonSerializer;

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
        private List<CityInfo> cityInfoList;


        // MARK: Edit table logic
        string EditRowId = null;
        int? selectedRow1 = null;
        int? selectedRow2 = null;
        private int? currentlySelectedRow = null;

        private DataTable originalView;
        private Dictionary<string, SortState?> sortStates = new Dictionary<string, SortState?>();
        public void InitializeOriginalTable(DataTable table)
        {
            originalView = table.Copy();
            originalView.Rows.RemoveAt(originalView.Rows.Count - 1);
        }


        private CityInfo newCityInfo = new CityInfo();
        private bool showAddCityInfoModal = false;

        private void OpenAddCityInfoModal()
        {
            showAddCityInfoModal = true;
        }

        private async Task AddCityInfo()
        {
            newCityInfo.id = Guid.NewGuid().ToString();
            await cosmosDbService.AddCityInfoAsync(newCityInfo);
            showAddCityInfoModal = false;
        }

        void BeginEdit(DataRow row)
        {
            EditRowId = row["cityDetailId"].ToString();
        }

        async void EndEdit(DataRow row)
        {
            CityInfo updatedCityInfo = new CityInfo
            {
                id = row["cityDetailId"].ToString(),
                Name = row["cityName"].ToString(),
                Population = Convert.ToInt32(row["cityPopulation"]),
                Country = row["cityCountry"].ToString(),
                Area = Convert.ToDouble(row["cityArea"]),
                Seen = row["citySeen"].ToString()
            };
            await cosmosDbService.UpdateCityInfoAsync(updatedCityInfo);
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
                        int rowCount = 6;
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

                for (int row = 2; row <= 6; row++)
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
                    id = row.ContainsKey("id") ? Convert.ToString(row["id"]) : Guid.NewGuid().ToString(),
                    Name = row.ContainsKey("Name") ? Convert.ToString(row["Name"]) : null,
                    Population = row.ContainsKey("Population") ? Convert.ToInt64(row["Population"]) : 0,
                    Country = row.ContainsKey("Country") ? Convert.ToString(row["Country"]) : null,
                    Area = row.ContainsKey("Area") ? Convert.ToDouble(row["Area"]) : 0,
                    Seen = row.ContainsKey("Seen") ? Convert.ToString(row["Seen"]) : "N",
                    DateAdded = row.ContainsKey("DateAdded") ? Convert.ToDateTime(row["DateAdded"]) : DateTime.UtcNow,
                };

                models.Add(model);
            }
            cityInfoList = models;
            InsertCityDetails(cityDetailTable, models);
            AddCityInfosAsync(models);
            InitializeOriginalTable(cityDetailTable);

            return models;
        }

        public static void InsertCityDetails(DataTable cityDT, List<CityInfo> cityRows)
        {
            double totPop = 0;
            double totArea = 0;
            foreach(CityInfo cityInfo in cityRows)
            {
                DataRow row = cityDT.NewRow();

                row["cityDetailId"] = cityInfo.id;
                row["cityName"] = cityInfo.Name;
                row["cityPopulation"] = cityInfo.Population;
                row["cityCountry"] = cityInfo.Country;
                row["cityArea"] = cityInfo.Area;
                row["citySeen"] = cityInfo.Seen;

                totPop += cityInfo.Population;
                totArea += cityInfo.Area;

                cityDT.Rows.Add(row);
            }

            if (cityRows.Count > 0)
            {
                DataRow averageRow = cityDT.NewRow();

                averageRow["cityDetailId"] = DBNull.Value;
                averageRow["cityName"] = "Average";
                averageRow["cityPopulation"] = totPop / cityRows.Count;
                averageRow["cityCountry"] = DBNull.Value;
                averageRow["cityArea"] = totArea / cityRows.Count;
                averageRow["citySeen"] = DBNull.Value;

                cityDT.Rows.Add(averageRow);
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

        private async void AddCityInfosAsync(List<CityInfo> cityInfos)
        {
            foreach (var cityInfo in cityInfos)
            {
                await cosmosDbService.AddCityInfoAsync(cityInfo);
            }
        }

        private void OnInputChange(ChangeEventArgs e, DataRow row, string columnName)
        {
            var updatedVal = e.Value.ToString();
            var cityInfoName = row["cityName"].ToString();
            var cityInfo = cityInfoList.FirstOrDefault(c => c.Name == cityInfoName);
            if (cityInfo != null)
            {
                var rrow = cityDetailTable.AsEnumerable().FirstOrDefault(r => r.Field<string>("cityName") == cityInfoName);
                if (rrow != null)
                {
                    rrow[columnName] = updatedVal;
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
            var cityInfo = cityInfoList.FirstOrDefault(c => c.id == cityInfoId);
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
                    cityInfo.Area = Convert.ToInt64(updatedValue);
                    break;
                case "citySeen":
                    cityInfo.Seen = updatedValue;
                    break;
            }
            cityInfo.DateAdded = DateTime.UtcNow;
            RecalculateAverages();
        }



        private bool IsLastRow(DataRow row)
        {
            return cityDetailTable.Rows.IndexOf(row) == cityDetailTable.Rows.Count - 1;
        }

        private async void DeleteCity(string cityInfoId)
        {
            bool isDeleted = false;
            foreach (DataRow row in cityDetailTable.Rows)
            {
                if (row["cityDetailId"].ToString() == cityInfoId)
                {
                    string cityName = row["cityName"].ToString();
                    await cosmosDbService.DeleteCityInfoAsync(cityName);
                    cityDetailTable.Rows.Remove(row);
                    isDeleted = true;
                    break;
                }
            }

            if (isDeleted)
            {
                RecalculateAverages();
            }
        }

        private void DeleteCityAndRefreshUI(string cityInfoId)
        {
            DeleteCity(cityInfoId);
            StateHasChanged();
        }

        private void RecalculateAverages()
        {
            double totalPopulation = 0;
            double totalArea = 0;
            int count = 0;

            foreach (DataRow row in cityDetailTable.Rows)
            {
                if (row["cityDetailId"] != DBNull.Value)
                {
                    totalPopulation += Convert.ToDouble(row["cityPopulation"]);
                    totalArea += Convert.ToDouble(row["cityArea"]);
                    count++;
                }
            }

            DataRow averageRow = cityDetailTable.AsEnumerable().FirstOrDefault(r => r["cityDetailId"] == DBNull.Value);

            if (averageRow == null)
            {
                averageRow = cityDetailTable.NewRow();
                averageRow["cityDetailId"] = DBNull.Value;
                cityDetailTable.Rows.Add(averageRow);
            }

            averageRow["cityName"] = "Average";
            averageRow["cityPopulation"] = count > 0 ? totalPopulation / count : 0;
            averageRow["cityCountry"] = DBNull.Value;
            averageRow["cityArea"] = count > 0 ? totalArea / count : 0;
            averageRow["citySeen"] = DBNull.Value;
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

            cityDetailTable.Rows.RemoveAt(cityDetailTable.Rows.Count - 1);
            
            if (!sortStates.ContainsKey(colName))
            {
                sortStates[colName] = SortState.None;
            }

            sortStates[colName] = sortStates[colName] switch
            {
                SortState.None => SortState.Ascending,
                SortState.Ascending => SortState.Descending,
                SortState.Descending => SortState.None,
                _ => SortState.None,
            };

            DataView view = cityDetailTable.DefaultView;
            switch (sortStates[colName])
            {
                case SortState.None:
                    cityDetailTable = originalView.Copy();
                    break;
                case SortState.Ascending:
                    view.Sort = $"{colName} ASC";
                    cityDetailTable = view.ToTable();
                    break;
                case SortState.Descending:
                    view.Sort = $"{colName} DESC";
                    cityDetailTable = view.ToTable();
                    break;
            }

            DataRow newRow = cityDetailTable.NewRow();
            newRow.ItemArray = lastRowData;
            cityDetailTable.Rows.Add(newRow);
        }

        public string GetSortIconClass(string columnName)
        {
            if (sortStates.ContainsKey(columnName))
            {
                return sortStates[columnName] switch
                {
                    SortState.Ascending => "oi oi-chevron-top",
                    SortState.Descending => "oi oi-chevron-bottom",
                    _ => "oi oi-elevator",
                };
            }
            return "oi oi-elevator";
        }

    }
}

enum SortState
{
    None,
    Ascending,
    Descending
}