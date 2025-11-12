using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.ServerPackets;
using System.Security.Cryptography;
using System.Text;
using Core.Database.Repositories.Api;

namespace Login.Server.Handlers;

/// <summary>
/// Handles CA_LOGIN packets for user authentication.
/// </summary>
[PacketHandler(PacketHeader.CA_LOGIN)]
public class LoginHandler : IPacketHandler<CA_LOGIN>
{
    private readonly ILogger<LoginHandler> _logger;
    private readonly ILoginRepository _loginRepository;

    public LoginHandler(
        ILogger<LoginHandler> logger,
        ILoginRepository loginRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loginRepository = loginRepository ?? throw new ArgumentNullException(nameof(loginRepository));
    }

    public async Task HandleAsync(ClientSession session, CA_LOGIN packet)
    {
        _logger.LogInformation("Login request from session {SessionId}: {Username}",
            session.SessionId, packet.Username);

        try
        {
            // Validate credentials against database
            var account = await _loginRepository.GetByEmailAsync(packet.Username);

            if (account == null)
            {
                _logger.LogWarning("Login failed for {Username} - account not found", packet.Username);
                session.Disconnect(DisconnectReason.Kicked);
                return;
            }

            // Verify password (assuming plain text for now, should use hashing in production)
            if (account.UserPass != packet.Password)
            {
                _logger.LogWarning("Login failed for {Username} - invalid password", packet.Username);
                
                // Update failed login attempts
                // await _loginRepository.IncrementLoginAttemptsAsync(account.AccountId);
                
                session.Disconnect(DisconnectReason.Kicked);
                return;
            }

            // Check account state
            if (account.State != 0)
            {
                _logger.LogWarning("Login failed for {Username} - account state {State}", 
                    packet.Username, account.State);
                session.Disconnect(DisconnectReason.Kicked);
                return;
            }

            // Check if account is banned
            // if (account.Unbantime.HasValue && account.Unbantime.Value > DateTime.UtcNow)
            // {
            //     _logger.LogWarning("Login failed for {Username} - account banned until {BanTime}", 
            //         packet.Username, account.Unbantime.Value);
            //     session.Disconnect(DisconnectReason.Kicked);
            //     return;
            // }

            // Update last login info
            // await _loginRepository.UpdateLastLoginAsync(
            //     account.AccountId, 
            //     session.RemoteEndPoint?.Address.ToString() ?? "unknown");

            // Generate session token
            var sessionToken = GenerateSessionToken();

            // var responsePacket = new AC_ACCEPT_LOGIN
            // {
            //     SessionToken = sessionToken,
            //     CharacterSlots = (byte)(account.CharacterSlots ?? 9)
            // };
            //
            // session.EnqueuePacket(responsePacket);

            _logger.LogInformation("Login successful for {Username} (AccountId: {AccountId}), token: {Token}",
                packet.Username, account.AccountId, sessionToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing login for {Username}", packet.Username);
            session.Disconnect(DisconnectReason.Kicked);
        }
    }

    private static int GenerateSessionToken()
    {
        return Random.Shared.Next(100000, 999999);
    }
}

