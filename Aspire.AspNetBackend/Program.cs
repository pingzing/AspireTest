using Aspire.AspNetBackend;
using Microsoft.Extensions.Caching.Distributed;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

// External clients
builder.Services.AddHttpClient<NodeApiClient>(client => {

    // Autoresolves based on name given over in apphost. Service discovery magic!
    client.BaseAddress = new("https+http://nodeApi");
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/nodeHello", async (NodeApiClient client, IDistributedCache redisCache) =>
{
    string? nodeHello;
    nodeHello = await redisCache.GetStringAsync("hello");
    if (nodeHello is null) 
    {
        app.Logger.LogWarning("Cache miss in /nodeHello!");
        nodeHello = await client.GetHello();
        await redisCache.SetStringAsync("hello", nodeHello);
    }
    return Results.Ok($"The nodeApi says: {nodeHello}");
})
.WithName("GetNodeHello")
.WithOpenApi();

app.Run();

