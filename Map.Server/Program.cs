using System.Reflection;
using Core.Database;
using Core.Server;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Map.Server;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Gm;
using Map.Server.Gm.Commands;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Scripting;
using Map.Server.Services;
using Map.Server.Session;
using Map.Server.Spawn;
using Map.Server.Visibility;
using Map.Server.Warps;
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
    return MapWorldRegistry.Load(serverConfig.MapDataPaths, serverConfig.Maps, logger);
});

// Static catalogs hydrated from Core.Database (rAthena use_sql_db: yes parity).
// IMobRepository / IItemRepository are Scoped; MobDb / IItemCatalog take
// IServiceScopeFactory and create scopes internally on load + Reload.
var dbConnectionString = configuration.GetConnectionString("GameDatabase")
    ?? throw new InvalidOperationException(
        "Missing connection string 'GameDatabase' in appsettings.json");
builder.Services.AddGameDatabase(dbConnectionString);
builder.Services.AddSingleton<IMobDb, MobDb>();
builder.Services.AddSingleton<IItemCatalog, ItemCatalog>();

// Register server state separately to avoid circular dependencies
builder.Services.AddSingleton<MapServerState>();
builder.Services.AddSingleton<IMapServerState>(sp => sp.GetRequiredService<MapServerState>());

// Register MapServerImpl
builder.Services.AddSingleton<MapServerImpl>();
builder.Services.AddSingleton<IServerReadiness>(sp => sp.GetRequiredService<MapServerImpl>());

// Entity infrastructure for MS1 gameplay (see .agents/migrations/map/entities.md).
builder.Services.AddSingleton<EntityIdAllocator>();
builder.Services.AddSingleton<IEntityRegistry, EntityRegistry>();

// Warps (see .agents/migrations/map/declarative-catalogs.md). At boot the
// service loads every warp row for hosted maps, marks NpcTrigger on each
// trigger-box cell (rAthena npc_setcells), and exposes O(1) lookup for the
// movement hot path. Must be registered before MovementService so the
// movement walk loop can resolve IWarpService.
builder.Services.AddSingleton<IWarpService, WarpService>();
builder.Services.AddSingleton<IWarpDispatcher, WarpDispatcher>();

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

// Scripting (see .agents/migrations/map/scripting/). At boot the host
// loads the esbuild bundle from scripts/dist/main.js; every register*()
// call in the bundle populates INpcRegistry. NpcSpawnService then places
// the scripted NPCs as entities. Phase 1 captures onClick/onTouch/...
// closures but does NOT invoke them — ContactNpcHandler logs and closes
// the dialog cleanly. Phase 2 wires the actual dispatcher.
var scriptOptions = new ScriptHostOptions();
configuration.GetSection("Scripting").Bind(scriptOptions);
builder.Services.AddSingleton(scriptOptions);
builder.Services.AddSingleton<INpcRegistry, NpcRegistry>();
builder.Services.AddSingleton<ScriptHost>();
builder.Services.AddSingleton<INpcSpawnService, NpcSpawnService>();
builder.Services.AddSingleton<Map.Server.Scripting.Dialog.IDialogDispatcher, Map.Server.Scripting.Dialog.DialogDispatcher>();

// Persistence: writes core character state (zeny / hp / sp / levels) and
// the three persistent var-reg scopes (perm / account / accountGlobal) to
// the DB. Map server writes directly via Core.Database; char-server IPC
// is no longer the route for player-state mutations.
builder.Services.AddSingleton<Map.Server.Persistence.IPlayerStateService, Map.Server.Persistence.PlayerStateService>();

// Inventory: loads each connecting character's inventory rows from the DB
// and emits the clif_inventorylist packet cascade so the client's bag UI
// populates. Slice 1 of the inventory/items work — see
// .agents/migrations/map/scripting/ for the broader plan.
builder.Services.AddSingleton<Map.Server.Inventory.IInventoryService, Map.Server.Inventory.InventoryService>();

// Status broadcast cascade (post-handoff). See
// .agents/migrations/map/initial-status-broadcast.md. The broadcaster
// emits the rAthena pc_authok / status_calc_pc(SCO_FIRST) packet stream
// matching the captured wire order.
builder.Services.AddSingleton<Map.Server.Status.StatusBroadcaster>();

// Renewal stat recalc (status.cpp:status_calc_pc / status_calc_mob).
// Owns BattleStats hydration for both players (at session enter / equip
// change / SC apply) and mobs (at spawn). Consumed by combat, skill, AI.
builder.Services.AddSingleton<Map.Server.Status.IStatusCalcService, Map.Server.Status.StatusCalcService>();

// Floor-item drop / pickup (see .agents/migrations/map/adjacent/items.md).
// MS3 first slice: the entity-on-the-floor lifecycle (drop, pickup, TTL
// despawn). Inventory persistence + item_db catalog land later.
builder.Services.AddSingleton<IItemDropService, ItemDropService>();

// Combat scaffolding (see .agents/migrations/map/adjacent/combat.md). MS3
// first slice: HP-mutation + death pipeline. Auto-attack loop and the
// full damage formula plug in later.
builder.Services.AddSingleton<IDamageService, DamageService>();

// GM commands. Each IGmCommand is registered as a singleton; the registry
// indexes them by Name at construction. ChatMessageHandler discovers them
// via DI.
builder.Services.AddSingleton<IGmCommand, WhereCommand>();
builder.Services.AddSingleton<IGmCommand, KillMobCommand>();
builder.Services.AddSingleton<IGmCommand, WarpCommand>();
builder.Services.AddSingleton<IGmCommand, DamageCommand>();
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
