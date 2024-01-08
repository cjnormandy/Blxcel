// using Microsoft.Azure.Cosmos;
// namespace BlazeApp.Data;

// public class CosmosDbService
// {
//     private readonly CosmosClient _cosmosClient;
//     private Container _container;

//     public CosmosDbService(CosmosClient cosmosClient)
//     {
//         _cosmosClient = cosmosClient;
//         _container = _cosmosClient.GetContainer("ToDoList", "Items");
//     }

//     public async Task<List<MyItem>> GetAllItemsAsync()
//     {
//         var sqlQueryText = "SELECT * FROM c";
//         QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
//         FeedIterator<MyItem> queryResultSetIterator = _container.GetItemQueryIterator<MyItem>(queryDefinition);

//         List<MyItem> results = new List<MyItem>();
//         while (queryResultSetIterator.HasMoreResults)
//         {
//             FeedResponse<MyItem> currentResultSet = await queryResultSetIterator.ReadNextAsync();
//             foreach (MyItem item in currentResultSet)
//             {
//                 results.Add(item);
//             }
//         }
//         return results;
//     }

//     public async Task AddItemAsync(MyItem newItem)
//     {
//         await _container.CreateItemAsync(newItem, new PartitionKey(newItem.Id));
//     }

//     public async Task AddCityInfoAsync(CityInfo cityInfo)
//     {
//         await _container.CreateItemAsync(cityInfo, new PartitionKey(cityInfo.Id));
//     }
// }
