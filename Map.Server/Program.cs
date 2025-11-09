using Core.Server;
using Map.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSerilog();
});

var logger = loggerFactory.CreateLogger<MapServerImpl>();

// Create server configuration
var serverConfig = new ServerConfiguration();
configuration.GetSection("Server").Bind(serverConfig);

// Create and start server
var server = new MapServerImpl(serverConfig, logger);

// Setup gRPC server
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.WebHost.UseUrls($"http://0.0.0.0:{serverConfig.GrpcPort}");

var app = builder.Build();
app.MapGrpcService<MapGrpcService>();

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
    
    Log.Information("MapServer is running at {FPS} FPS. Press Ctrl+C to stop.", serverConfig.TargetFPS);
    
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