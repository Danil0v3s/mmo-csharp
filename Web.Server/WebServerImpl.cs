using Core.Server;
using Serilog;

namespace Web.Server;

public class WebServerImpl : AbstractServer
{
    private WebApplication? _app;

    public WebServerImpl(ServerConfiguration configuration, ILogger<WebServerImpl> logger)
        : base("WebServer", configuration, logger)
    {
    }

    protected override async Task OnStartingAsync(CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        
        // Configure services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // Configure logging
        builder.Host.UseSerilog();

        // Configure URLs
        builder.WebHost.UseUrls($"http://0.0.0.0:{Configuration.Port}");

        _app = builder.Build();

        // Configure middleware
        _app.UseSwagger();
        _app.UseSwaggerUI();
        _app.UseCors();
        _app.UseAuthorization();
        _app.MapControllers();

        // Map API endpoints
        MapApiEndpoints(_app);

        // Start the web server
        _ = Task.Run(async () => await _app.RunAsync(cancellationToken), cancellationToken);

        Logger.LogInformation("WebServer started on port {Port}", Configuration.Port);

        await Task.CompletedTask;
    }

    protected override async Task OnStoppingAsync(CancellationToken cancellationToken)
    {
        if (_app != null)
        {
            await _app.StopAsync(cancellationToken);
            await _app.DisposeAsync();
            _app = null;
        }

        Logger.LogInformation("WebServer stopped");
    }

    private void MapApiEndpoints(WebApplication app)
    {
        // Health check endpoint
        app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", server = "WebServer" }));

        // Server status endpoint
        app.MapGet("/api/status", async () =>
        {
            var status = new
            {
                LoginServer = await CheckServerStatusAsync("LoginServer"),
                CharServer = await CheckServerStatusAsync("CharServer"),
                MapServer = await CheckServerStatusAsync("MapServer")
            };
            return Results.Ok(status);
        });

        // Account endpoints
        app.MapPost("/api/account/register", (RegisterRequest request) =>
        {
            Logger.LogInformation("Account registration: {Username}", request.Username);
            
            // TODO: Create account in database
            var response = new RegisterResponse(
                Success: true,
                AccountId: new Random().Next(10000, 99999),
                Message: "Account created successfully"
            );

            return Results.Ok(response);
        });

        app.MapPost("/api/account/login", (LoginRequest request) =>
        {
            Logger.LogInformation("Web API login: {Username}", request.Username);
            
            // TODO: Validate credentials
            var response = new LoginResponse(
                Success: true,
                SessionToken: Guid.NewGuid().ToString(),
                Message: "Login successful"
            );

            return Results.Ok(response);
        });

        // Server info endpoint
        app.MapGet("/api/servers", () =>
        {
            var servers = new
            {
                login = new { host = "localhost", port = 5001 },
                character = new { host = "localhost", port = 5002 },
                map = new { host = "localhost", port = 5003 }
            };
            return Results.Ok(servers);
        });
    }

    private Task<string> CheckServerStatusAsync(string serverName)
    {
        try
        {
            var channel = IpcClient.GetChannel(serverName);
            if (channel != null)
            {
                return Task.FromResult("online");
            }
            return Task.FromResult("offline");
        }
        catch
        {
            return Task.FromResult("offline");
        }
    }
}

public record RegisterRequest(string Username, string Password, string Email);
public record RegisterResponse(bool Success, long AccountId, string Message);

public record LoginRequest(string Username, string Password);
public record LoginResponse(bool Success, string SessionToken, string Message);

