using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add a Redis distributed cache
var redis = builder.AddRedis("redis");

// Add a NodeJS API with the given name, working dir, and 'npm' command
var nodeApi = builder.AddNpmApp("nodeApi", "../AspireTest.NodeApi", "start:dev")
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile(); // TODO: Gotta actually write this Dockerfile

// Workaround to make sure NodeJS app can communicate with other resources
if (builder.Environment.IsDevelopment() && builder.Configuration["DOTNET_LAUNCH_PROFILE"] == "https")
{
    // Disable TLS certificate validation in development, see https://github.com/dotnet/aspire/issues/3324 for more details.
    nodeApi.WithEnvironment("NODE_TLS_REJECT_UNAUTHORIZED", "0");
}

// Add an ASP.NET Core API project that uses the Redis Cache and the NodeJS API
var aspNetApi = builder.AddProject<Projects.Aspire_AspNetBackend>("AspNetCoreApi")
    .WithReference(nodeApi)
    .WithReference(redis);

builder.Build().Run();
