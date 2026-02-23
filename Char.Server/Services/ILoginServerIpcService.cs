using Core.Server.IPC;

namespace Char.Server.Services;

/// <summary>
/// IPC operations for communicating with the Login server.
/// Separated from CharServerImpl to allow clean DI.
/// </summary>
public interface ILoginServerIpcService
{
    Task<CharacterServerAuthResponse?> AuthenticateAccountAsync(
        int accountId,
        int loginId1,
        int loginId2,
        uint sex,
        int requestId,
        int charServerId,
        CancellationToken cancellationToken = default);

    Task NotifyAccountStatusAsync(
        int accountId,
        int charServerId,
        bool online,
        CancellationToken cancellationToken = default);

    Task<AccountDataResponse?> RequestFullAccountDataAsync(
        int accountId,
        CancellationToken cancellationToken = default);

    Task<AccountInfoResponse?> RequestDetailedAccountInfoAsync(
        int accountId,
        CancellationToken cancellationToken = default);

    Task<AccountStateUpdateResponse?> UpdateAccountStateAsync(
        int accountId,
        uint state,
        CancellationToken cancellationToken = default);

    Task<AccountBanResponse?> BanAccountAsync(
        int accountId,
        int durationSeconds,
        CancellationToken cancellationToken = default);

    Task<AccountUnbanResponse?> UnbanAccountAsync(
        int accountId,
        CancellationToken cancellationToken = default);

    Task<AccountEmailChangeResponse?> ChangeAccountEmailAsync(
        int accountId,
        string currentEmail,
        string newEmail,
        CancellationToken cancellationToken = default);

    Task<AccountSexChangeResponse?> ChangeAccountSexAsync(
        int accountId,
        CancellationToken cancellationToken = default);

    Task<AccountPincodeUpdateResponse?> UpdateAccountPincodeAsync(
        int accountId,
        string pincode,
        CancellationToken cancellationToken = default);

    Task<AccountPincodeAuthFailResponse?> NotifyPincodeAuthFailAsync(
        int accountId,
        CancellationToken cancellationToken = default);

    Task<GlobalAccRegUpdateResponse?> UpdateGlobalAccountRegistersAsync(
        int accountId,
        IEnumerable<GlobalAccRegEntry> entries,
        CancellationToken cancellationToken = default);

    Task<GlobalAccRegFetchResponse?> GetGlobalAccountRegistersAsync(
        int accountId,
        long charId,
        CancellationToken cancellationToken = default);

    Task<AccountVipDataResponse?> RequestVipDataAsync(
        int accountId,
        uint flags,
        int durationSeconds,
        int mapServerId,
        CancellationToken cancellationToken = default);

    Task<CharacterServerRegistrationResponse?> RegisterCharacterServerAsync(
        string username,
        string password,
        string serverName,
        string serverAddress,
        ushort socketPort,
        uint serverType,
        bool newServer,
        CancellationToken cancellationToken = default);

    Task UpdateUserCountAsync(
        int serverId,
        uint userCount,
        CancellationToken cancellationToken = default);

    Task UpdateServerAddressAsync(
        int serverId,
        uint ip,
        CancellationToken cancellationToken = default);

    Task SetAllOfflineAsync(
        int serverId,
        CancellationToken cancellationToken = default);

    Task SyncOnlineAccountsAsync(
        int serverId,
        IEnumerable<int> accountIds,
        CancellationToken cancellationToken = default);
}
