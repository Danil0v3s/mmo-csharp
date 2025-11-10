using System.Collections.Concurrent;
using Core.Server.Network;
using Core.Server.Packets;
using Microsoft.Extensions.Logging;

namespace Map.Server;

/// <summary>
/// Main packet handler coordinator for the Map Server.
/// Registers all individual packet handlers.
/// </summary>
public class MapPacketHandler : PacketHandler
{
    private readonly ConcurrentDictionary<long, PlayerEntity> _players;
    private readonly ConcurrentDictionary<Guid, long> _sessionToCharacter;
    private readonly SessionManager _sessionManager;

    public MapPacketHandler(
        ILogger logger,
        ConcurrentDictionary<long, PlayerEntity> players,
        ConcurrentDictionary<Guid, long> sessionToCharacter,
        SessionManager sessionManager) : base(logger)
    {
        _players = players;
        _sessionToCharacter = sessionToCharacter;
        _sessionManager = sessionManager;
    }

    protected override void RegisterHandlers()
    {
        // Register individual packet handlers
        // TODO: Implement CZ_ENTER, CZ_REQUEST_MOVE, CZ_REQUEST_CHAT packet classes
        // Once implemented, register them like this:
        RegisterHandler(PacketHeader.CZ_ENTER, new Handlers.EnterMapHandler(Logger, _players, _sessionToCharacter, _sessionManager));
        // RegisterHandler(PacketHeader.CZ_REQUEST_MOVE, new Handlers.MoveRequestHandler(Logger, _players, _sessionToCharacter));
        // RegisterHandler(PacketHeader.CZ_REQUEST_CHAT, new Handlers.ChatMessageHandler(Logger, _players, _sessionToCharacter, _sessionManager));
        
        Logger.LogInformation("Registered {Count} packet handler(s) for Map Server", Registry.HandlerCount);
    }
}

