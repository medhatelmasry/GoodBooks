using System.Reflection;
using Google.Protobuf.WellKnownTypes;

// using Infrastructure.Module;

// Console.WriteLine("Using Infrastructure.Module.ModuleManager");
// var moduleManager = new ModuleManager();

// Console.WriteLine("Infrastructure project is referenced.");

var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("gdb-sql-server")
                 .AddDatabase("gdb-db");

// Create MigrationService first - it needs to run before the API
var migrations = builder.AddProject<Projects.MigrationService>("migrations")
    .WithReference(sqlServer)
    .WaitFor(sqlServer);

// API waits for both SQL Server AND migrations to complete
var apiService = builder.AddProject<Projects.Api>("api")
        .WithReference(sqlServer)
        .WithHttpEndpoint(port: 8001)
        .WaitFor(sqlServer)
        .WaitFor(migrations);

builder.AddProject<Projects.AccountGoWeb>("mvc")
        .WithHttpEndpoint(port: 8000)
        .WithReference(apiService)
        .WaitFor(apiService);

builder.Build().Run();

