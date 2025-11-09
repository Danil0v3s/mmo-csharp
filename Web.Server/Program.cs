using Core.Server;
using Web.Server;
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

var logger = loggerFactory.CreateLogger<WebServerImpl>();

// Create server configuration
var serverConfig = new ServerConfiguration();
configuration.GetSection("Server").Bind(serverConfig);

// Create and start server
var server = new WebServerImpl(serverConfig, logger);

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
    
    Log.Information("WebServer is running on http://localhost:{Port}", serverConfig.Port);
    Log.Information("Swagger UI available at http://localhost:{Port}/swagger", serverConfig.Port);
    Log.Information("Press Ctrl+C to stop.");
    
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
    Log.CloseAndFlush();
}