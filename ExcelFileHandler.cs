using OfficeOpenXml;
using System.Data;
using BlazeApp.Data;
public class ExcelFileHandler
{
    private readonly CityInfoDbService cityInfoDbService;
    public ExcelFileHandler(CityInfoDbService cityInfoDbService)
    {
        this.cityInfoDbService = cityInfoDbService;
    }
    public List<Dictionary<string, object>> ReadExcelToDictionaryAsync(Stream fileStream, List<string> cHeaders, List<Dictionary<string, object>> res, List<CityInfo> cityInfos, DataTable cityDT)
    {

        using (var package = new ExcelPackage(fileStream))
        {
            var worksheet = package.Workbook.Worksheets[0];

            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                cHeaders.Add(worksheet.Cells[1, col].Text);
            }

            for (int row = 2; row <= 6; row++)
            {
                var rowDict = new Dictionary<string, object>();
                for (int col = 1; col <= cHeaders.Count; col++)
                {
                    var key = cHeaders[col - 1];
                    var value = worksheet.Cells[row, col].Value;
                    rowDict.Add(key, value);
                }
                res.Add(rowDict);
            }
        }
        ConvertDictionaryToModel(res, cityInfos, cityDT);

        return res;
    }

    public void ConvertDictionaryToModel(List<Dictionary<string, object>> data, List<CityInfo> cities, DataTable cDT)
    {
        if(data == null) throw new ArgumentException(nameof(data));
        if(cities == null) throw new ArgumentException(nameof(cities));
        if(cDT == null) throw new ArgumentException(nameof(cDT));

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

        cities.AddRange(models);

        InsertCityDetails(cDT, models);
        AddCityInfosAsync(models);
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

    private async void AddCityInfosAsync(List<CityInfo> cityInfos)
    {
        foreach (var cityInfo in cityInfos)
        {
            await cityInfoDbService.AddCityInfoAsync(cityInfo);
        }
    }

    
}