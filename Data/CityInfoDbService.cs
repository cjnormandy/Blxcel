using Microsoft.Azure.Cosmos;
namespace BlazeApp.Data;

public class CityInfoDbService
{
    private readonly CosmosClient _cosmosClient;
    private Container _container;
    private Container _container2;

    public CityInfoDbService(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;
        _container = _cosmosClient.GetContainer("CitiesDb", "Cities");
        _container2 = _cosmosClient.GetContainer("CitiesDb", "CountryColorMapping");
    }

    public async Task AddCityInfoAsync(CityInfo cityInfo)
    {
        var existingCity = await GetCityInfoByNameAsync(cityInfo.Name);
        if (existingCity == null)
        {
            cityInfo.id = Guid.NewGuid().ToString();
            await _container.CreateItemAsync(cityInfo, new PartitionKey(cityInfo.id));
        }
        else
        {
            try
            {
                // Construct a new SQL query using the country from the fetched city
                string sqlQuery = $@"SELECT m.Color
                                    FROM CountryColorMapping m
                                    WHERE m.Country = '{existingCity.Country}'";

                // Execute the query
                var queryDefinition = new QueryDefinition(sqlQuery);
                var queryResultSetIterator = _container2.GetItemQueryIterator<CountryColor>(queryDefinition);

                while (queryResultSetIterator.HasMoreResults)
                {
                    var currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (CountryColor countryColor in currentResultSet)
                    {
                        // Update the color in cityInf with the fetched color
                        cityInfo.id = existingCity.id;
                        cityInfo.Color = countryColor.Color;

                        // Replace the item in the container with the updated cityInfo
                        await _container.ReplaceItemAsync(cityInfo, existingCity.id, new PartitionKey(existingCity.id));
                    }
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine("Item not found");
            }
        }
    }

    public async Task<List<CityInfo>> GetAllCityInfosAsync()
    {
        var cityInfos = new List<CityInfo>();
        var query = _container.GetItemQueryIterator<CityInfo>(new QueryDefinition("SELECT * FROM c"));
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            cityInfos.AddRange(response.Resource);
        }
        return cityInfos;
    }

    public async Task<CityInfo?> GetCityInfoByNameAsync(string cityName)
    {
        var query = _container.GetItemQueryIterator<CityInfo>(
            new QueryDefinition("SELECT * FROM c WHERE c.Name = @Name")
            .WithParameter("@Name", cityName));

        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            foreach (var city in response)
            {
                return city;
            }
        }

        return null;
    }

    public async Task UpdateCityInfoAsync(CityInfo updatedCityInfo)
    {
        var existingCity = await GetCityInfoByNameAsync(updatedCityInfo.Name);
        if (existingCity == null)
        {
            throw new ArgumentException($"City with the name {updatedCityInfo.Name} does not exist.");
        }

        try
        {
            // Construct a new SQL query using the country from the fetched city
            string sqlQuery = $@"SELECT m.Color
                                FROM CountryColorMapping m
                                WHERE m.Country = '{existingCity.Country}'";

            // Execute the query
            var queryDefinition = new QueryDefinition(sqlQuery);
            var queryResultSetIterator = _container2.GetItemQueryIterator<CountryColor>(queryDefinition);

            while (queryResultSetIterator.HasMoreResults)
            {
                var currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (CountryColor countryColor in currentResultSet)
                {
                    // Update the color in cityInf with the fetched color
                    updatedCityInfo.id = existingCity.id;
                    updatedCityInfo.Color = countryColor.Color;

                    // Replace the item in the container with the updated cityInfo
                    await _container.ReplaceItemAsync(updatedCityInfo, updatedCityInfo.id, new PartitionKey(updatedCityInfo.id));
                }
            }
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.WriteLine("Item not found");
        }
        
        // updatedCityInfo.id = existingCity.id;
        // await _container.ReplaceItemAsync(updatedCityInfo, updatedCityInfo.id, new PartitionKey(updatedCityInfo.id));
        
    }

    public async Task DeleteCityInfoAsync(string cityName)
    {
        var cityInfo = await GetCityInfoByNameAsync(cityName);
        if (cityInfo != null)
        {
            await _container.DeleteItemAsync<CityInfo>(cityInfo.id, new PartitionKey(cityInfo.id));
        }
        else
        {
            throw new ArgumentException($"City with the name {cityName} does not exist.");
        }
    }


}
