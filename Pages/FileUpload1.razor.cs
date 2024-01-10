using System.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazeApp.Pages
{
    public partial class FileUpload1
    {
        private List<IBrowserFile> loadedFiles = new();
        private List<string> columnHeaders = new();
        private List<Dictionary<string, object>> result = new();
        private bool isLoading;

        // MARK: DataTable Logic
        [Inject]
        private DataTableService dataTableService { get; set; }
        private DataTable cityDetailTable = new();
        private List<CityInfo> cityInfoList = new();
        [Inject]
        private ExcelFileHandler excelFileHandler { get; set; }


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

            cityDetailTable = dataTableService.CreateCityDetailTable();

            result.Clear();
            loadedFiles.Clear();
            cityInfoList.Clear();
            columnHeaders.Clear();
            cityDetailTable.Clear();

            foreach (var file in e.GetMultipleFiles())
            {
                using var memoryStream = new MemoryStream();
                await file.OpenReadStream().CopyToAsync(memoryStream);

                memoryStream.Position = 0;
                excelFileHandler.ReadExcelToDictionaryAsync(memoryStream, columnHeaders, result, cityInfoList, cityDetailTable);
                InitializeOriginalTable(cityDetailTable);

            }

            isLoading = false;
            
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
            // RecalculateAverages();
            dataTableService.CalculateAverages(cityDetailTable);
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
                // RecalculateAverages();
                dataTableService.CalculateAverages(cityDetailTable);
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