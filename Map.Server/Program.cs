using System.Reflection;
using Core.Server;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Map.Server;
using Map.Server.Entities;
using Map.Server.Gm;
using Map.Server.Gm.Commands;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Services;
using Map.Server.Session;
using Map.Server.Spawn;
using Map.Server.Visibility;
using Map.Server.World;
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
var serverConfig = new MapServerConfiguration();
configuration.GetSection("Server").Bind(serverConfig);

// Configure services
builder.Services.AddSingleton<ServerConfiguration>(serverConfig);
builder.Services.AddSingleton(serverConfig);
builder.Services.AddSingleton<ILogger>(sp => sp.GetRequiredService<ILogger<Program>>());

// Register decoupled services
builder.Services.AddSingleton<ServerConnectionService>();
builder.Services.AddSingleton<IServerConnectionService>(sp => sp.GetRequiredService<ServerConnectionService>());
builder.Services.AddSingleton<ICharServerIpcService, CharServerIpcService>();
builder.Services.AddSingleton<IPlayerMapService, PlayerMapService>();

// World data: load the configured maps once at startup and expose via IMapWorldRegistry.
builder.Services.AddSingleton<IMapWorldRegistry>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    return MapWorldRegistry.Load(serverConfig.MapDataPath, serverConfig.Maps, logger);
});

// Mob database: rAthena mob_db.yml (+ mob_db2.yml overrides) parsed once at
// startup (see .agents/migrations/map/mob-db.md).
builder.Services.AddSingleton<IMobDb>(sp => new MobDb(
    serverConfig.MobDbPath,
    string.IsNullOrEmpty(serverConfig.MobDbOverridePath) ? null : serverConfig.MobDbOverridePath,
    sp.GetRequiredService<ILogger<MobDb>>()));

// Register server state separately to avoid circular dependencies
builder.Services.AddSingleton<MapServerState>();
builder.Services.AddSingleton<IMapServerState>(sp => sp.GetRequiredService<MapServerState>());

// Register MapServerImpl
builder.Services.AddSingleton<MapServerImpl>();

// Entity infrastructure for MS1 gameplay (see .agents/migrations/map/entities.md).
builder.Services.AddSingleton<EntityIdAllocator>();
builder.Services.AddSingleton<IEntityRegistry, EntityRegistry>();

// Movement (see .agents/migrations/map/movement.md). Walk steps are scheduled
// through Core.Timer's Scheduler; the service binds entities → walk timers.
builder.Services.AddSingleton<IMovementService, MovementService>();

// Visibility / AOI broadcast (see .agents/migrations/map/visibility.md).
builder.Services.AddSingleton<IPacketDispatcher, SessionPacketDispatcher>();
builder.Services.AddSingleton<IVisibilityService, VisibilityService>();

// Session lifecycle: detects dead TCP sessions, tears down the bound
// PlayerEntity, broadcasts vanish, and fires the char-server LeaveMap IPC.
// Polled once per map tick from MapServerImpl.UpdateGameLogicAsync.
builder.Services.AddSingleton<MapSessionLifecycle>();

// Mob spawn (see .agents/migrations/map/spawn.md). Registry is the
// collection of declared spawn entries; service drives initial spawn,
// idle wander, and respawn timing.
builder.Services.AddSingleton<IMobSpawnRegistry, MobSpawnRegistry>();
builder.Services.AddSingleton<IMobSpawnService, MobSpawnService>();

// Floor-item drop / pickup (see .agents/migrations/map/adjacent/items.md).
// MS3 first slice: the entity-on-the-floor lifecycle (drop, pickup, TTL
// despawn). Inventory persistence + item_db catalog land later.
builder.Services.AddSingleton<IItemDropService, ItemDropService>();

// GM commands. Each IGmCommand is registered as a singleton; the registry
// indexes them by Name at construction. ChatMessageHandler discovers them
// via DI.
builder.Services.AddSingleton<IGmCommand, WhereCommand>();
builder.Services.AddSingleton<IGmCommand, KillMobCommand>();
builder.Services.AddSingleton<IGmCommand, WarpCommand>();
builder.Services.AddSingleton<IGmCommandRegistry, GmCommandRegistry>();

// Core services
builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton<PacketSystem>();
builder.Services.AddSingleton<IPacketFactory>(sp => sp.GetRequiredService<PacketSystem>().Factory);
builder.Services.AddSingleton<IPacketSizeRegistry>(sp => sp.GetRequiredService<PacketSystem>().Registry);

// Auto-register all packet handlers from assembly
var handlerTypes = typeof(MapServerImpl).Assembly.GetTypes()
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
app.MapGrpcService<MapGrpcService>();

// Get server instance from DI
var server = app.Services.GetRequiredService<MapServerImpl>();

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
