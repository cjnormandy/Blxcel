using BlazeApp.Data;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

var cosmosDbEndpoint = builder.Configuration["CosmosDb:Endpoint"];
var cosmosDbKey = builder.Configuration["CosmosDb:Key"];

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();

// Add Cosmos DB client as a singleton service
builder.Services.AddSingleton<CosmosClient>(s =>
{
    return new CosmosClient(cosmosDbEndpoint, cosmosDbKey, new CosmosClientOptions());
});
builder.Services.AddSingleton<CityInfoDbService>();

builder.Services.AddSingleton<ExcelFileHandler>();

builder.Services.AddSingleton<DataTableService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
