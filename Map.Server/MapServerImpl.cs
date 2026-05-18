using System.Net;
using System.Net.Sockets;
using Core.Server;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Map.Server.Items;
using Map.Server.Scripting;
using Map.Server.Services;
using Map.Server.Session;
using Map.Server.Spawn;

namespace Map.Server;

public class MapServerImpl : GameLoopServer, IServerReadiness
{
    /// <summary>
    /// Map is "ready" only once it has finished its DB-heavy boot (mob_db,
    /// item_db, world cache), its game loop is ticking, the map list is
    /// registered with the char server, AND the client-facing address
    /// (MapIp:Port) is pushed to char — without the address the char
    /// server has the map registered but can't build HC_SEND_MAP_DATA, so
    /// CH_SELECT_CHAR falls through to RejectAuthResult.
    /// </summary>
    public bool IsReady => State == ServerState.Running && _registered && _addressSynced;

    private Socket? _listenerSocket;
    private readonly PacketHandlerRegistry _handlerRegistry;
    private readonly MapServerState _serverState;
    private readonly IPlayerMapService _playerMapService;
    private readonly ICharServerIpcService _charServerIpc;
    private readonly MapSessionLifecycle _lifecycle;
    private readonly IMobSpawnService _mobSpawn;
    private readonly IItemDropService _itemDrops;
    private readonly ScriptHost _scriptHost;
    private readonly INpcSpawnService _npcSpawn;
    private readonly MapServerConfiguration _mapConfiguration;
    private DateTime _nextRegistrationAttemptUtc = DateTime.MinValue;
    private DateTime _nextKeepAliveUtc = DateTime.MinValue;
    private DateTime _nextUserCountSyncUtc = DateTime.MinValue;
    private DateTime _nextAutosaveUtc = DateTime.MinValue;
    private volatile bool _registered;
    private volatile bool _addressSynced;
    private int _lastReportedUserCount = -1;

    public MapServerImpl(
        ServerConfiguration configuration,
        ILogger<MapServerImpl> logger,
        IServiceProvider serviceProvider,
        PacketSystem packetSystem,
        SessionManager sessionManager,
        ServerConnectionService connectionService,
        MapServerState serverState,
        IPlayerMapService playerMapService,
        ICharServerIpcService charServerIpc,
        MapSessionLifecycle lifecycle,
        IMobSpawnService mobSpawn,
        IItemDropService itemDrops,
        ScriptHost scriptHost,
        INpcSpawnService npcSpawn)
        : base("MapServer", configuration, logger, packetSystem, sessionManager)
    {
        _handlerRegistry = new PacketHandlerRegistry(serviceProvider, logger);
        _handlerRegistry.DiscoverAndRegisterFromCallingAssembly();
        _serverState = serverState;
        _playerMapService = playerMapService;
        _charServerIpc = charServerIpc;
        _lifecycle = lifecycle;
        _mobSpawn = mobSpawn;
        _itemDrops = itemDrops;
        _scriptHost = scriptHost;
        _npcSpawn = npcSpawn;
        _mapConfiguration = (MapServerConfiguration)configuration;

        // Wire up the connection service to use this server's connection manager
        connectionService.SetConnectionManager(ServerConnections);
    }

    public override async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await base.StartAsync(cancellationToken);
        _serverState.SetState(State);

        // Initial mob population per the configured spawn registry. Idempotent
        // — re-runs on tick wouldn't double-spawn, but we do it once at boot to
        // pre-fill the map before any client connects.
        _mobSpawn.SpawnInitial();

        // Load the TypeScript scripting bundle. Every register*() call in the
        // bundle populates INpcRegistry; NpcSpawnService then places the
        // scripted NPCs as entities. Boot fails loudly if the bundle has bad
        // registrations (duplicate names, missing required fields, etc.); a
        // missing bundle is a warning + zero NPCs (acceptable during early dev).
        _scriptHost.LoadEntryPoint();
        _npcSpawn.SpawnInitial();
    }

    public override async Task StopAsync(CancellationToken cancellationToken = default)
    {
        // P6 — graceful shutdown: batch-save any online players, then tell the char server
        // to clear online flags for everyone on this map server.
        await SaveAllOnlinePlayersAsync(cancellationToken);
        if (_registered)
        {
            try
            {
                await _charServerIpc.SetAllCharactersOfflineAsync(_mapConfiguration.ServerId, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to SetAllCharactersOffline on shutdown");
            }
        }

        await base.StopAsync(cancellationToken);
        _serverState.SetState(State);
    }

    protected override async Task StartTcpListenerAsync(CancellationToken cancellationToken)
    {
        _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listenerSocket.Bind(new IPEndPoint(IPAddress.Any, Configuration.Port));
        _listenerSocket.Listen(Configuration.MaxConnections);

        Logger.LogInformation("MapServer TCP listener started on port {Port}", Configuration.Port);

        _ = Task.Run(async () => await AcceptClientsAsync(cancellationToken), cancellationToken);

        await Task.CompletedTask;
    }

    protected override async Task StopTcpListenerAsync(CancellationToken cancellationToken)
    {
        if (_listenerSocket != null)
        {
            _listenerSocket.Close();
            _listenerSocket.Dispose();
            _listenerSocket = null;
        }

        Logger.LogInformation("MapServer TCP listener stopped");
        await Task.CompletedTask;
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listenerSocket != null)
        {
            try
            {
                var clientSocket = await _listenerSocket.AcceptAsync(cancellationToken);
                var session = SessionManager.CreateSession<MapSessionData>(clientSocket);
                Logger.LogInformation("Client connected: {SessionId}", session.SessionId);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error accepting client");
            }
        }
    }

    protected override async Task ProcessIncomingPacketsAsync(double deltaTime, CancellationToken cancellationToken)
    {
        foreach (var session in SessionManager.GetAllSessions())
        {
            await _handlerRegistry.ProcessSessionPacketsAsync(session, Logger);
        }
    }

    protected override async Task UpdateGameLogicAsync(double deltaTime, CancellationToken cancellationToken)
    {
        // P6 infrastructure timers (rAthena chrif equivalents):
        //   1. Register maps once on first reachable char server (chmapif_parse_getmapname)
        //   2. Periodic KeepAlive (chmapif_parse_keepalive)
        //   3. Periodic user-count sync (chmapif_parse_regmapuser / getusercount)
        //   4. Periodic autosave of online characters (chrif_save loop)
        await EnsureRegisteredOnCharServerAsync(cancellationToken);
        await SendKeepAliveIfDueAsync(cancellationToken);
        await SyncUserCountIfDueAsync(cancellationToken);
        await AutosaveIfDueAsync(cancellationToken);
        // Sweep dead client sessions before the SessionManager removes them so
        // we can broadcast vanish + run LeaveMap IPC while the EntityId is
        // still bound. Idempotent; sets MapSessionData.CleanupCompleted.
        await _lifecycle.SweepAsync(cancellationToken);
        // Mob wander + pending respawn promotion.
        _mobSpawn.Tick();
        // Floor-item auto-despawn.
        _itemDrops.Tick();
    }

    private async Task EnsureRegisteredOnCharServerAsync(CancellationToken cancellationToken)
    {
        if (_registered && _addressSynced) return;
        if (DateTime.UtcNow < _nextRegistrationAttemptUtc) return;
        _nextRegistrationAttemptUtc = DateTime.UtcNow.AddSeconds(5);

        try
        {
            if (!_registered)
            {
                var response = await _charServerIpc.RegisterMapServerMapsAsync(
                    _mapConfiguration.ServerId,
                    _mapConfiguration.Maps,
                    cancellationToken);
                if (response?.Success == true)
                {
                    _registered = true;
                    Logger.LogInformation(
                        "Registered {Count} maps with char server (server id {ServerId})",
                        _mapConfiguration.Maps.Count, _mapConfiguration.ServerId);
                }
            }

            // Address sync: char needs MapIp:Port to populate HC_SEND_MAP_DATA
            // when the client picks a character whose last_map lives on this
            // server. Without it the registry has the map name but no endpoint,
            // and the select handler falls through to RejectAuthResult.
            if (_registered && !_addressSynced)
            {
                if (!TryConvertIpv4ToUInt(_mapConfiguration.MapIp, out var ip))
                {
                    Logger.LogWarning(
                        "Cannot sync map server address: MapIp '{MapIp}' is not a valid IPv4",
                        _mapConfiguration.MapIp);
                }
                else
                {
                    var addressResponse = await _charServerIpc.UpdateMapServerAddressAsync(
                        _mapConfiguration.ServerId,
                        ip,
                        (uint)_mapConfiguration.Port,
                        cancellationToken);
                    if (addressResponse?.Success == true)
                    {
                        _addressSynced = true;
                        Logger.LogInformation(
                            "Synced map server address {Ip}:{Port} with char server (server id {ServerId})",
                            _mapConfiguration.MapIp, _mapConfiguration.Port, _mapConfiguration.ServerId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Char server not reachable for map registration; will retry");
        }
    }

    private static bool TryConvertIpv4ToUInt(string ipAddress, out uint ip)
    {
        ip = 0;
        if (!IPAddress.TryParse(ipAddress, out var parsed) || parsed.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = parsed.GetAddressBytes();
        ip = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
        return true;
    }

    private async Task SendKeepAliveIfDueAsync(CancellationToken cancellationToken)
    {
        if (!_registered || DateTime.UtcNow < _nextKeepAliveUtc) return;
        _nextKeepAliveUtc = DateTime.UtcNow.AddSeconds(Math.Max(_mapConfiguration.KeepAliveInterval, 5));

        try
        {
            await _charServerIpc.KeepAliveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "KeepAlive to char server failed");
        }
    }

    private async Task SyncUserCountIfDueAsync(CancellationToken cancellationToken)
    {
        if (!_registered || DateTime.UtcNow < _nextUserCountSyncUtc) return;
        _nextUserCountSyncUtc = DateTime.UtcNow.AddSeconds(Math.Max(_mapConfiguration.UserCountSyncInterval, 5));

        var count = _playerMapService.Count;
        if (count == _lastReportedUserCount) return;

        try
        {
            await _charServerIpc.RegisterMapServerUserCountAsync(_mapConfiguration.ServerId, count, cancellationToken);
            _lastReportedUserCount = count;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "User-count sync to char server failed");
        }
    }

    private async Task AutosaveIfDueAsync(CancellationToken cancellationToken)
    {
        if (!_registered || DateTime.UtcNow < _nextAutosaveUtc) return;
        _nextAutosaveUtc = DateTime.UtcNow.AddSeconds(Math.Max(_mapConfiguration.AutosaveInterval, 30));

        await SaveAllOnlinePlayersAsync(cancellationToken);
    }

    private async Task SaveAllOnlinePlayersAsync(CancellationToken cancellationToken)
    {
        foreach (var player in _playerMapService.GetAllPlayers())
        {
            try
            {
                await _charServerIpc.SaveCharacterStateAsync(
                    player.AccountId,
                    player.CharacterId,
                    setOfflineAfterSave: false,
                    finalSave: false,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex,
                    "Autosave failed for character {CharId} (acc {AccountId})",
                    player.CharacterId, player.AccountId);
            }
        }
    }

    protected override async Task FlushOutgoingPacketsAsync(CancellationToken cancellationToken)
    {
        foreach (var session in SessionManager.GetAllSessions())
        {
            await session.FlushPacketsAsync();
        }
    }
}

// The legacy PlayerEntity that lived here has been replaced by
// Map.Server.Entities.PlayerEntity (which inherits from Entity and integrates
// with IEntityRegistry's spatial index). IPlayerMapService is now a facade
// over IEntityRegistry for IPC-driven code paths.
