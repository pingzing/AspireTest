using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddNpmApp("nodeApi", "../AspireTest.NodeApi", "start:dev")
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
