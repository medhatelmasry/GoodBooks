using System.Reflection;
using Google.Protobuf.WellKnownTypes;

// using Infrastructure.Module;

// Console.WriteLine("Using Infrastructure.Module.ModuleManager");
// var moduleManager = new ModuleManager();

// Console.WriteLine("Infrastructure project is referenced.");

var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("gdb-sql-server")
                 .AddDatabase("gdb-db");

// read environment variable for connection string
var apiService = builder.AddProject<Projects.Api>("api")
        .WithReference(sqlServer)
        .WithHttpEndpoint(port: 8001)
        .WaitFor(sqlServer);

builder.AddProject<Projects.AccountGoWeb>("mvc")
        .WithHttpEndpoint(port: 8000)
        .WithReference(apiService)
        .WaitFor(apiService);

var migrations = builder.AddProject<Projects.MigrationService>("migrations")
    .WithReference(sqlServer)
    .WaitFor(sqlServer);

// Wait for migrations to complete before starting API
apiService.WaitFor(migrations);

builder.Build().Run();

