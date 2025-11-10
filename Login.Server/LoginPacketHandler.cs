using Core.Server.Network;
using Core.Server.Packets;
using Microsoft.Extensions.Logging;

namespace Login.Server;

/// <summary>
/// Main packet handler coordinator for the Login Server.
/// Registers all individual packet handlers.
/// </summary>
public class LoginPacketHandler : PacketHandler
{
    public LoginPacketHandler(ILogger logger) : base(logger)
    {
    }

    protected override void RegisterHandlers()
    {
        // Register individual packet handlers
        RegisterHandler(PacketHeader.CA_LOGIN, new Handlers.LoginPacketHandler(Logger));
        
        Logger.LogInformation("Registered {Count} packet handler(s) for Login Server", Registry.HandlerCount);
    }
}

