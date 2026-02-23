using Core.Server;
using Core.Server.IPC;
using Grpc.Core;
using Login.Server.UseCase;

namespace Login.Server.Handlers;

/// <summary>
/// Handles character server registration via gRPC.
/// This replaces the packet-based approach used in C++ with gRPC communication.
/// </summary>
public class CharServerGrpcHandler(
    ILogger logger,
    ILoginMmoAuth loginMmoAuth,
    ICharServerRegistry charServerRegistry
)
{
    /// <summary>
    /// Handles character server registration request via gRPC
    /// </summary>
    public async Task<CharacterServerRegistrationResponse> RegisterCharacterServerAsync(
        CharacterServerRegistrationRequest request,
        ServerCallContext context)
    {
        logger.LogInformation("Character server registration request from {ServerName} at {ServerAddress}",
            request.ServerName, request.ServerAddress);

        // Create a temporary session data object for authentication
        var sessionData = new TempAuthSessionData
        {
            UserId = request.Username,
            Password = request.Password,
            ClientType = 0 // Character server connection doesn't use client type
        };

        try
        {
            // Authenticate the character server using the same logic as regular login
            // but with isServer=true
            var result = await loginMmoAuth.ExecuteAsync(new ILoginMmoAuth.Input(sessionData, true));

            if (result.ResultCode == -1 && sessionData.Sex == 'S' && sessionData.AccountId < 5) // MAX_SERVERS = 5
            {
                // Verify that the character server ID isn't already in use
                var existingServer = charServerRegistry.GetCharServer(sessionData.AccountId);
                if (existingServer != null && !string.IsNullOrEmpty(existingServer.Name))
                {
                    logger.LogWarning("Character server registration refused: server ID {ServerId} already in use", sessionData.AccountId);
                    return new CharacterServerRegistrationResponse
                    {
                        Success = false,
                        ErrorMessage = "Server ID already in use",
                        ResultCode = 3
                    };
                }

                // Parse the server IP address
                uint serverIp = ConvertIpToUInt32(request.ServerAddress.Split(':')[0]);
                ushort socketPort = ResolveSocketPort(request);

                // Add the character server to our tracking
                charServerRegistry.AddCharServer(
                    sessionData.AccountId,
                    request.ServerName,
                    serverIp,
                    socketPort,
                    (ushort)request.ServerType,
                    request.NewServer ? (ushort)1 : (ushort)0);

                logger.LogInformation("Character server '{ServerName}' (account {AccountId}) registered successfully",
                    request.ServerName, sessionData.AccountId);

                return new CharacterServerRegistrationResponse
                {
                    Success = true,
                    ServerId = sessionData.AccountId,
                    ResultCode = 0
                };
            }
            else
            {
                logger.LogWarning("Character server registration refused for {Username} (result: {ResultCode}, sex: {Sex}, accountId: {AccountId})",
                    request.Username, result.ResultCode, sessionData.Sex, sessionData.AccountId);

                return new CharacterServerRegistrationResponse
                {
                    Success = false,
                    ErrorMessage = "Authentication failed",
                    ResultCode = 3
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing character server registration for {Username}", request.Username);

            return new CharacterServerRegistrationResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ResultCode = 3
            };
        }
    }

    private uint ConvertIpToUInt32(string ipAddress)
    {
        var ip = System.Net.IPAddress.Parse(ipAddress);
        var bytes = ip.GetAddressBytes();
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    private ushort ResolveSocketPort(CharacterServerRegistrationRequest request)
    {
        if (request.SocketPort > 0 && request.SocketPort <= ushort.MaxValue)
        {
            return (ushort)request.SocketPort;
        }

        // Legacy fallback for older clients that only sent IP:Port in server_address.
        var addressParts = request.ServerAddress.Split(':');
        if (addressParts.Length > 1 && ushort.TryParse(addressParts[1], out var parsedPort))
        {
            return parsedPort;
        }

        throw new FormatException("Character server registration missing a valid socket port.");
    }
}

/// <summary>
/// Temporary session data class for authentication purposes only
/// </summary>
public class TempAuthSessionData : ILoginMmoAuth.ITempSessionData
{
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int PasswordEnc { get; set; } = 0;
    public string Md5Key { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public char Sex { get; set; } = '\0';
    public byte ClientType { get; set; }
    public byte GroupId { get; set; }

    // These are needed to match the interface but aren't used for auth
    public string WebAuthToken { get; set; } = string.Empty;
    public int LoginId1 { get; set; }
    public int LoginId2 { get; set; }
}
