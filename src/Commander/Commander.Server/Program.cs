using Commander.Core.Factories;
using Commander.Core.Ports;
using Commander.Infrastructure.Adapters;
using Commander.Infrastructure.Configuration;
using Commander.Server.Services;
using Docker.DotNet;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddSingleton<IRunnerPort, DockerRunnerAdapter>();
builder.Services.AddSingleton<IJobDefinitionFactory, JobDefinitionFactory>();
builder.Services.AddSingleton<IJobStore, InMemoryJobStore>();
builder.Services.AddSingleton<IDockerClient>(_ =>
    new DockerClientConfiguration(DockerConfiguration.GetDockerUri())
      .CreateClient());

builder.Services.Configure<DockerRunnerOptions>(
    builder.Configuration.GetSection("DockerRunner")
);

var app = builder.Build();
Console.WriteLine(builder.Configuration["DockerRunner:Image"]);

if (app.Environment.IsDevelopment())
{
  app.MapGrpcReflectionService();
}

app.MapGrpcService<OrchestratorService>();
app.MapGrpcService<RunnerService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();
