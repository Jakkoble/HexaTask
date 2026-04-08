using Commander.Core.Factories;
using Commander.Core.Ports;
using Commander.Infrastructure.Adapters;
using Commander.Server.Services;
using Commander.Server.Store;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddSingleton<IRunnerPort, DockerRunnerAdapter>();
builder.Services.AddSingleton<JobDefinitionFactory>();
builder.Services.AddSingleton<IJobStore, InMemoryJobStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.MapGrpcReflectionService();
}

app.MapGrpcService<OrchestratorService>();
app.MapGrpcService<RunnerService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();
