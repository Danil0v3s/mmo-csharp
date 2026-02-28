using System.Reflection;
using Char.Server;
using Char.Server.Services;
using Core.Database;
using Core.Server;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Microsoft.AspNetCore.Server.Kestrel.Core;
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
var serverConfig = new CharServerConfiguration();
configuration.GetSection("Server").Bind(serverConfig);

// Configure services
builder.Services.AddSingleton<ServerConfiguration>(serverConfig);
builder.Services.AddSingleton(serverConfig);
builder.Services.AddSingleton<ILogger>(sp => sp.GetRequiredService<ILogger<Program>>());

// Register decoupled services
builder.Services.AddSingleton<ServerConnectionService>();
builder.Services.AddSingleton<IServerConnectionService>(sp => sp.GetRequiredService<ServerConnectionService>());
builder.Services.AddSingleton<ILoginServerIpcService, LoginServerIpcService>();
builder.Services.AddSingleton<IMapAuthTicketService, MapAuthTicketService>();
builder.Services.AddSingleton<IMapServerRegistryService, MapServerRegistryService>();
builder.Services.AddTransient<ICharacterListFlowService, CharacterListFlowService>();

// Register server state separately to avoid circular dependencies
builder.Services.AddSingleton<CharServerState>();
builder.Services.AddSingleton<ICharServerState>(sp => sp.GetRequiredService<CharServerState>());

// Register CharServerImpl - also provides ICharServerRuntime for gRPC service
builder.Services.AddSingleton<CharServerImpl>();

// Register database services used by character list/select handlers.
var connectionString = configuration.GetConnectionString("GameDatabase")
                       ?? throw new InvalidOperationException("Database connection string 'GameDatabase' not found in configuration");
builder.Services.AddGameDatabase(connectionString);

// Core services
builder.Services.AddSingleton<PacketSystem>();
builder.Services.AddSingleton<IPacketFactory>(sp => sp.GetRequiredService<PacketSystem>().Factory);
builder.Services.AddSingleton<IPacketSizeRegistry>(sp => sp.GetRequiredService<PacketSystem>().Registry);
builder.Services.AddSingleton<SessionManager>();

// Auto-register all packet handlers from assembly
var handlerTypes = typeof(CharServerImpl).Assembly.GetTypes()
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
builder.WebHost.ConfigureKestrel(options =>
{
    // gRPC over cleartext (h2c) requires an HTTP/2-only endpoint.
    options.ListenAnyIP(serverConfig.GrpcPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();
app.MapGrpcService<CharGrpcService>();

// Get server instance from DI
var server = app.Services.GetRequiredService<CharServerImpl>();

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
    
    Log.Information("CharServer is running. Press Ctrl+C to stop.");
    
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
