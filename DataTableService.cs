using System.Data;

public class DataTableService
{
    public DataTable CreateCityDetailTable()
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
            new DataColumn("cityColor", typeof(string)),
        };

        cityDetailDT.Columns.AddRange(cols);

        return cityDetailDT;      
    }

    public void CalculateAverages(DataTable dataTable)
    {
        double totalPopulation = 0;
        double totalArea = 0;
        int count = 0;

        foreach (DataRow row in dataTable.Rows)
        {
            if (row["cityDetailId"] != DBNull.Value)
            {
                totalPopulation += Convert.ToDouble(row["cityPopulation"]);
                totalArea += Convert.ToDouble(row["cityArea"]);
                count++;
            }
        }

        DataRow averageRow = dataTable.AsEnumerable().FirstOrDefault(r => r["cityDetailId"] == DBNull.Value);

        if (averageRow == null)
        {
            averageRow = dataTable.NewRow();
            averageRow["cityDetailId"] = DBNull.Value;
            dataTable.Rows.Add(averageRow);
        }

        averageRow["cityName"] = "Average";
        averageRow["cityPopulation"] = count > 0 ? totalPopulation / count : 0;
        averageRow["cityCountry"] = DBNull.Value;
        averageRow["cityArea"] = count > 0 ? totalArea / count : 0;
        averageRow["citySeen"] = DBNull.Value;
    }
}

public enum SortState
{
    None,
    Ascending,
    Descending
}
