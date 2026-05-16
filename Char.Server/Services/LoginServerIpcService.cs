using Core.Server;
using Core.Server.IPC;

namespace Char.Server.Services;

public class LoginServerIpcService(
    IServerConnectionService connectionService,
    ILogger<LoginServerIpcService> logger
) : ILoginServerIpcService
{
    private ServerSession? GetLoginSession()
    {
        return connectionService.GetSessionsByType(ServerType.Login).FirstOrDefault();
    }

    public async Task<CharacterServerAuthResponse?> AuthenticateAccountAsync(
        int accountId,
        int loginId1,
        int loginId2,
        uint sex,
        int requestId,
        int charServerId,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return null;

        var client = new LoginService.LoginServiceClient(session.Channel);
        return await client.AuthenticateAccountForCharServerAsync(new CharacterServerAuthRequest
        {
            AccountId = accountId,
            LoginId1 = loginId1,
            LoginId2 = loginId2,
            Sex = sex,
            RequestId = requestId,
            CharServerId = (uint)Math.Max(charServerId, 0)
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountOnlineAnywhereResponse?> IsAccountOnlineAnywhereAsync(
        int accountId,
        int excludeCharServerId,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return null;

        var client = new LoginService.LoginServiceClient(session.Channel);
        try
        {
            return await client.IsAccountOnlineAnywhereAsync(new AccountOnlineAnywhereRequest
            {
                AccountId = accountId,
                ExcludeCharServerId = excludeCharServerId
            }, cancellationToken: cancellationToken);
        }
        catch (Grpc.Core.RpcException ex)
        {
            logger.LogWarning(ex, "IsAccountOnlineAnywhere RPC failed for account {AccountId}", accountId);
            return null;
        }
    }

    public async Task NotifyAccountStatusAsync(
        int accountId,
        int charServerId,
        bool online,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return;

        var client = new LoginService.LoginServiceClient(session.Channel);
        await client.NotifyAccountStatusAsync(new AccountStatusUpdateRequest
        {
            AccountId = accountId,
            CharServerId = Math.Max(charServerId, 0),
            Online = online
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountDataResponse?> RequestFullAccountDataAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return null;

        var client = new LoginService.LoginServiceClient(session.Channel);
        return await client.GetFullAccountDataAsync(new AccountDataRequest
        {
            AccountId = accountId
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountInfoResponse?> RequestDetailedAccountInfoAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return null;

        var client = new LoginService.LoginServiceClient(session.Channel);
        return await client.GetAccountInfoAsync(new AccountInfoRequest
        {
            AccountId = accountId
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountStateUpdateResponse?> UpdateAccountStateAsync(
        int accountId,
        uint state,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return null;

        var client = new LoginService.LoginServiceClient(session.Channel);
        return await client.UpdateAccountStateAsync(new AccountStateUpdateRequest
        {
            AccountId = accountId,
            State = state
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountBanResponse?> BanAccountAsync(
        int accountId,
        int durationSeconds,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return null;

        var client = new LoginService.LoginServiceClient(session.Channel);
        return await client.BanAccountAsync(new AccountBanRequest
        {
            AccountId = accountId,
            DurationSeconds = durationSeconds
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountUnbanResponse?> UnbanAccountAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return null;

        var client = new LoginService.LoginServiceClient(session.Channel);
        return await client.UnbanAccountAsync(new AccountUnbanRequest
        {
            AccountId = accountId
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountEmailChangeResponse?> ChangeAccountEmailAsync(
        int accountId,
        string currentEmail,
        string newEmail,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return null;

        var client = new LoginService.LoginServiceClient(session.Channel);
        return await client.ChangeAccountEmailAsync(new AccountEmailChangeRequest
        {
            AccountId = accountId,
            CurrentEmail = currentEmail,
            NewEmail = newEmail
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountSexChangeResponse?> ChangeAccountSexAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return null;

        var client = new LoginService.LoginServiceClient(session.Channel);
        return await client.ChangeAccountSexAsync(new AccountSexChangeRequest
        {
            AccountId = accountId
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountPincodeUpdateResponse?> UpdateAccountPincodeAsync(
        int accountId,
        string pincode,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return null;

        var client = new LoginService.LoginServiceClient(session.Channel);
        return await client.UpdateAccountPincodeAsync(new AccountPincodeUpdateRequest
        {
            AccountId = accountId,
            Pincode = pincode
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountPincodeAuthFailResponse?> NotifyPincodeAuthFailAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return null;

        var client = new LoginService.LoginServiceClient(session.Channel);
        return await client.NotifyPincodeAuthFailAsync(new AccountPincodeAuthFailRequest
        {
            AccountId = accountId
        }, cancellationToken: cancellationToken);
    }

    public async Task<GlobalAccRegUpdateResponse?> UpdateGlobalAccountRegistersAsync(
        int accountId,
        IEnumerable<GlobalAccRegEntry> entries,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return null;

        var request = new GlobalAccRegUpdateRequest { AccountId = accountId };
        request.Entries.AddRange(entries);

        var client = new LoginService.LoginServiceClient(session.Channel);
        return await client.UpdateGlobalAccountRegistersAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<GlobalAccRegFetchResponse?> GetGlobalAccountRegistersAsync(
        int accountId,
        long charId,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return null;

        var client = new LoginService.LoginServiceClient(session.Channel);
        return await client.GetGlobalAccountRegistersAsync(new GlobalAccRegFetchRequest
        {
            AccountId = accountId,
            CharId = charId
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountVipDataResponse?> RequestVipDataAsync(
        int accountId,
        uint flags,
        int durationSeconds,
        int mapServerId,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return null;

        var client = new LoginService.LoginServiceClient(session.Channel);
        return await client.RequestVipDataAsync(new AccountVipDataRequest
        {
            AccountId = accountId,
            Flags = flags,
            DurationSeconds = durationSeconds,
            MapServerId = mapServerId
        }, cancellationToken: cancellationToken);
    }

    public async Task<CharacterServerRegistrationResponse?> RegisterCharacterServerAsync(
        string username,
        string password,
        string serverName,
        string serverAddress,
        ushort socketPort,
        uint serverType,
        bool newServer,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return null;

        var client = new LoginService.LoginServiceClient(session.Channel);
        return await client.RegisterCharacterServerAsync(new CharacterServerRegistrationRequest
        {
            Username = username,
            Password = password,
            ServerName = serverName,
            ServerAddress = serverAddress,
            SocketPort = socketPort,
            ServerType = serverType,
            NewServer = newServer
        }, cancellationToken: cancellationToken);
    }

    public async Task UpdateUserCountAsync(
        int serverId,
        uint userCount,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return;

        var client = new LoginService.LoginServiceClient(session.Channel);
        await client.UpdateCharacterServerUserCountAsync(new CharacterServerUserCountUpdateRequest
        {
            ServerId = serverId,
            UserCount = userCount
        }, cancellationToken: cancellationToken);
    }

    public async Task UpdateServerAddressAsync(
        int serverId,
        uint ip,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return;

        var client = new LoginService.LoginServiceClient(session.Channel);
        await client.UpdateCharacterServerAddressAsync(new CharacterServerAddressUpdateRequest
        {
            ServerId = serverId,
            Ip = ip
        }, cancellationToken: cancellationToken);
    }

    public async Task SetAllOfflineAsync(
        int serverId,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return;

        try
        {
            var client = new LoginService.LoginServiceClient(session.Channel);
            await client.SetAllOfflineForCharacterServerAsync(new CharacterServerSetAllOfflineRequest
            {
                ServerId = serverId
            }, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to set all offline on login server for char server {ServerId}", serverId);
        }
    }

    public async Task UnregisterCharacterServerAsync(
        int serverId,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return;

        try
        {
            var client = new LoginService.LoginServiceClient(session.Channel);
            await client.UnregisterCharacterServerAsync(new CharacterServerUnregisterRequest
            {
                ServerId = serverId
            }, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to unregister char server {ServerId} on login server", serverId);
        }
    }

    public async Task SyncOnlineAccountsAsync(
        int serverId,
        IEnumerable<int> accountIds,
        CancellationToken cancellationToken = default)
    {
        var session = GetLoginSession();
        if (session?.IsConnected != true) return;

        var client = new LoginService.LoginServiceClient(session.Channel);
        var request = new CharacterServerOnlineSyncRequest { ServerId = serverId };
        request.AccountIds.AddRange(accountIds);
        await client.SyncOnlineAccountsAsync(request, cancellationToken: cancellationToken);
    }
}
