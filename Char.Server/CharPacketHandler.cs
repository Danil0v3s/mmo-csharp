using Core.Server.Network;
using Core.Server.Packets;
using Microsoft.Extensions.Logging;

namespace Char.Server;

/// <summary>
/// Main packet handler coordinator for the Char Server.
/// Registers all individual packet handlers.
/// </summary>
public class CharPacketHandler : PacketHandler
{
    public CharPacketHandler(ILogger logger) : base(logger)
    {
    }

    protected override void RegisterHandlers()
    {
        // Register individual packet handlers
        // TODO: Implement CH_CHARLIST_REQ, CH_MAKE_CHAR, CH_SELECT_CHAR packet classes
        // Once implemented, register them like this:
        RegisterHandler(PacketHeader.CH_CHARLIST_REQ, new Handlers.CharacterListRequestHandler(Logger));
        // RegisterHandler(PacketHeader.CH_MAKE_CHAR, new Handlers.CharacterCreateHandler(Logger));
        // RegisterHandler(PacketHeader.CH_SELECT_CHAR, new Handlers.CharacterSelectHandler(Logger));
        
        Logger.LogInformation("Registered {Count} packet handler(s) for Char Server", Registry.HandlerCount);
    }
}

