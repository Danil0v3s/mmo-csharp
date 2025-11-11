using System.Reflection;
using Core.Server;
using Core.Server.Network;
using Login.Server;
using Serilog;

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
var serverConfig = new ServerConfiguration();
configuration.GetSection("Server").Bind(serverConfig);

// Configure services
builder.Services.AddSingleton(serverConfig);
builder.Services.AddSingleton<Microsoft.Extensions.Logging.ILogger>(sp => sp.GetRequiredService<ILogger<Program>>());
builder.Services.AddSingleton<LoginServerImpl>();

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