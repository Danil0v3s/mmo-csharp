using Core.Server.Network;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.ServerPackets;
using Microsoft.Extensions.Logging;

namespace Login.Server.Handlers;

/// <summary>
/// Handles CA_LOGIN packets for authentication.
/// </summary>
public class LoginPacketHandler : IPacketHandler<CA_LOGIN>
{
    private readonly ILogger _logger;

    public LoginPacketHandler(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(ClientSession session, CA_LOGIN packet)
    {
        _logger.LogInformation("Login request from session {SessionId}: {Username}",
            session.SessionId, packet.Username);

        // TODO: Validate credentials against database
        // For now, accept any non-empty credentials
        bool success = !string.IsNullOrWhiteSpace(packet.Username) &&
                      !string.IsNullOrWhiteSpace(packet.Password);

        if (success)
        {
            var sessionToken = new Random().Next(100000, 999999);

            var responsePacket = new AC_ACCEPT_LOGIN
            {
                SessionToken = sessionToken,
                CharacterSlots = 9
            };

            session.EnqueuePacket(responsePacket);

            _logger.LogInformation("Login successful for {Username}, token: {Token}",
                packet.Username, sessionToken);
        }
        else
        {
            // TODO: Implement AC_REFUSE_LOGIN packet
            _logger.LogWarning("Login failed for session {SessionId} - invalid credentials", session.SessionId);
            session.Disconnect(DisconnectReason.Kicked);
        }

        await Task.CompletedTask;
    }
}

