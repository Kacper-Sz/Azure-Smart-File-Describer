using Azure.WebApp.Services.AI;
using Microsoft.Azure.Cosmos;
using Project.Services.Database;
using Project.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();

builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var connectionString = builder.Configuration["CosmosDb:ConnectionString"];
    return new CosmosClient(connectionString);
});
builder.Services.AddScoped<ICosmosDbService, CosmosDbService>();
builder.Services.AddScoped<IAzureComputerVisionService, AzureComputerVisionService>();
builder.Services.AddScoped<IAzureDocumentIntelligenceService, AzureDocumentIntelligenceService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
