using System.Reflection;
using Core.Database;
using Core.Database.Context;
using Core.Database.Seeds;
using Core.Server;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Timer;
using Login.Server;
using Login.Server.UseCase;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

// Setup configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// Setup logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// Setup gRPC and DI container
var builder = WebApplication.CreateBuilder(args);

// Create server configuration
var serverConfig = new LoginServerConfiguration();
configuration.GetSection("Server").Bind(serverConfig);

// Configure services
builder.Services.AddSingleton<ServerConfiguration>(serverConfig);
builder.Services.AddSingleton(serverConfig);
builder.Services.AddSingleton<ILogger>(sp => sp.GetRequiredService<ILogger<Program>>());
builder.Services.AddSingleton<TimerManager>();
builder.Services.AddSingleton<LoginServerImpl>();
builder.Services.AddSingleton<PacketSystem>();
builder.Services.AddSingleton<IPacketFactory>(sp => sp.GetRequiredService<PacketSystem>().Factory);
builder.Services.AddSingleton<IPacketSizeRegistry>(sp => sp.GetRequiredService<PacketSystem>().Registry);
builder.Services.AddSingleton<SessionManager>();

builder.Services.AddTransient<ILoginMmoAuth, LoginMmoAuth>();

// Register database services
var connectionString = configuration.GetConnectionString("GameDatabase")
                       ?? throw new InvalidOperationException("Database connection string 'GameDatabase' not found in configuration");
builder.Services.AddGameDatabase(connectionString);

// Auto-register all packet handlers from assembly
var handlerTypes = typeof(LoginServerImpl).Assembly.GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract)
    .Where(t => t.GetCustomAttribute<PacketHandlerAttribute>() != null);

foreach (var handlerType in handlerTypes)
{
    builder.Services.AddTransient(handlerType);
}

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// Configure gRPC
builder.Services.AddGrpc();
builder.WebHost.UseUrls($"http://0.0.0.0:{serverConfig.GrpcPort}");

var app = builder.Build();
app.MapGrpcService<LoginGrpcService>();

// Apply migrations and seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        Log.Information("Initializing database...");

        var context = services.GetRequiredService<GameDbContext>();

        // Apply any pending migrations
        await context.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully");

        // Seed database from SQL scripts
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var seeder = new DatabaseSeeder(context, loggerFactory.CreateLogger<DatabaseSeeder>());
        await seeder.SeedAsync();

        Log.Information("Database initialization completed");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "An error occurred while initializing the database");
        throw;
    }
}

// Get server instance from DI
var server = app.Services.GetRequiredService<LoginServerImpl>();

// Start gRPC server in background
_ = Task.Run(async () => await app.RunAsync());

// Setup graceful shutdown
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await server.StartAsync(cts.Token);

    Log.Information("LoginServer is running. Press Ctrl+C to stop.");

    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    Log.Information("Shutdown requested...");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Server terminated unexpectedly");
}
finally
{
    await server.StopAsync();
    await app.StopAsync();
    Log.CloseAndFlush();
}