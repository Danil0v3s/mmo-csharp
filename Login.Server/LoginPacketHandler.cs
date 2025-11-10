using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.ServerPackets;
using Microsoft.Extensions.Logging;

namespace Login.Server;

/// <summary>
/// Handles all incoming packets for the Login Server.
/// </summary>
public class LoginPacketHandler : PacketHandler
{
    public LoginPacketHandler(ILogger logger) : base(logger)
    {
    }

    public override async Task HandlePacketAsync(ClientSession session, IncomingPacket packet)
    {
        // Use pattern matching for clean, type-safe packet handling
        switch (packet)
        {
            case CA_LOGIN login:
                await HandleLoginAsync(session, login);
                break;

            default:
                Logger.LogWarning("Unhandled packet type: {PacketType} (Header: 0x{Header:X4}) from session {SessionId}",
                    packet.GetType().Name, (short)packet.Header, session.SessionId);
                break;
        }
    }

    private async Task HandleLoginAsync(ClientSession session, CA_LOGIN loginPacket)
    {
        Logger.LogInformation("Login request from session {SessionId}: {Username}",
            session.SessionId, loginPacket.Username);

        // TODO: Validate credentials against database
        // For now, accept any non-empty credentials
        bool success = !string.IsNullOrWhiteSpace(loginPacket.Username) &&
                      !string.IsNullOrWhiteSpace(loginPacket.Password);

        if (success)
        {
            var sessionToken = new Random().Next(100000, 999999);

            var responsePacket = new AC_ACCEPT_LOGIN
            {
                SessionToken = sessionToken,
                CharacterSlots = 9
            };

            SendPacket(session, responsePacket);

            Logger.LogInformation("Login successful for {Username}, token: {Token}",
                loginPacket.Username, sessionToken);
        }
        else
        {
            // TODO: Implement AC_REFUSE_LOGIN packet
            Logger.LogWarning("Login failed for session {SessionId} - invalid credentials", session.SessionId);
            session.Disconnect(DisconnectReason.Kicked);
        }

        await Task.CompletedTask;
    }
}

