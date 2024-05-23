//using Aspire.Hosting;

using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var nodeApi = builder.AddNpmApp("nodeApi", "../AspireTest.NodeApi", "start:dev")
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

if (builder.Environment.IsDevelopment() && builder.Configuration["DOTNET_LAUNCH_PROFILE"] == "https")
{
    // Disable TLS certificate validation in development, see https://github.com/dotnet/aspire/issues/3324 for more details.
    nodeApi.WithEnvironment("NODE_TLS_REJECT_UNAUTHORIZED", "0");
}

builder.Build().Run();
