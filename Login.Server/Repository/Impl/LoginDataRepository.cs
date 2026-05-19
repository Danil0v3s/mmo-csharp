using System.Net;
using Core.Database.Repositories.Api;
using Core.Timer;
using Login.Server.Model;
using Login.Server.Repository.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Login.Server.Repository.Impl;

internal class LoginDataRepository(
    IServiceScopeFactory scopeFactory,
    LoginServerConfiguration configuration,
    ILogger<LoginDataRepository> logger) : ILoginDataRepository
{
    private static readonly Dictionary<AccountId, OnlineLoginData> OnlineLoginDataDictionary = new();
    private static readonly Dictionary<AccountId, AuthNode> AuthNodeDictionary = new();

    public OnlineLoginData? GetOnlineUser(int accountId)
    {
        lock (OnlineLoginDataDictionary)
        {
            OnlineLoginDataDictionary.TryGetValue(new AccountId(accountId), out var data);
            return data;
        }
    }

    public async Task<OnlineLoginData> AddOnlineUser(int charServer, int accountId)
    {
        OnlineLoginData onlineLoginData;
        lock (OnlineLoginDataDictionary)
        {
            var accId = new AccountId(accountId);
            if (!OnlineLoginDataDictionary.TryGetValue(accId, out var existingOnlineLoginData))
            {
                onlineLoginData = new OnlineLoginData(
                    CharServer: charServer,
                    AccountId: accId,
                    WaitingDisconnect: Scheduler.InvalidTimer,
                    VipTimeoutTid: Scheduler.InvalidTimer
                );
            }
            else
            {
                onlineLoginData = existingOnlineLoginData with { CharServer = charServer };

                if (onlineLoginData.WaitingDisconnect != Scheduler.InvalidTimer)
                {
                    Scheduler.Cancel(onlineLoginData.WaitingDisconnect);
                    onlineLoginData = onlineLoginData with { WaitingDisconnect = Scheduler.InvalidTimer };
                }
            }
        }

        await UpdateAccountWebTokenEnabled(accountId, true);
        return onlineLoginData;
    }

    public void SetOnlineUserCharServer(int accountId, int charServer)
    {
        lock (OnlineLoginDataDictionary)
        {
            var accId = new AccountId(accountId);
            if (OnlineLoginDataDictionary.TryGetValue(accId, out var onlineLoginData))
            {
                Update(onlineLoginData with { CharServer = charServer });
            }
            else
            {
                Update(new OnlineLoginData(
                    AccountId: accId,
                    CharServer: charServer,
                    WaitingDisconnect: Scheduler.InvalidTimer,
                    VipTimeoutTid: Scheduler.InvalidTimer));
            }
        }
    }

    public async Task RemoveOnlineUser(int accountId)
    {
        lock (OnlineLoginDataDictionary)
        {
            var accId = new AccountId(accountId);
            if (OnlineLoginDataDictionary.TryGetValue(accId, out var onlineLoginData))
            {
                if (onlineLoginData.WaitingDisconnect != Scheduler.InvalidTimer)
                {
                    Scheduler.Cancel(onlineLoginData.WaitingDisconnect);
                }

                OnlineLoginDataDictionary.Remove(accId);
            }
        }

        // rAthena account.cpp:account_db_sql_disable_webtoken schedules a
        // timer for login_config.disable_webtoken_delay ms; the timer's
        // callback (account_disable_webtoken_timer) re-checks
        // login_get_online_user and only flips the column to 0 if the
        // user is still offline. The intent is to survive a fast
        // disconnect+reconnect without invalidating the token mid-handoff.
        // Delay <= 0 keeps the legacy behavior (immediate flush).
        var delayMs = configuration.DisableWebTokenDelay;
        if (delayMs <= 0)
        {
            await UpdateAccountWebTokenEnabled(accountId, false);
            return;
        }

        Scheduler.Schedule(
            DisableWebTokenIfStillOffline,
            state: accountId,
            dueTime: TimeSpan.FromMilliseconds(delayMs));
    }

    private async ValueTask DisableWebTokenIfStillOffline(object? state, TimerId _, long __)
    {
        var accountId = (int)state!;
        bool stillOnline;
        lock (OnlineLoginDataDictionary)
        {
            stillOnline = OnlineLoginDataDictionary.ContainsKey(new AccountId(accountId));
        }
        if (stillOnline) return;
        try
        {
            await UpdateAccountWebTokenEnabled(accountId, false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Delayed web-token disable failed for account {AccountId}", accountId);
        }
    }

    public async Task<int> RemoveOnlineUsersByCharServer(int charServer)
    {
        List<int> accountIdsToRemove;
        lock (OnlineLoginDataDictionary)
        {
            accountIdsToRemove = OnlineLoginDataDictionary
                .Where(p => p.Value.CharServer == charServer)
                .Select(p => p.Key.Value)
                .ToList();
        }

        foreach (var accountId in accountIdsToRemove)
        {
            await RemoveOnlineUser(accountId);
        }

        return accountIdsToRemove.Count;
    }

    public AuthNode? GetAuthNode(int accountId)
    {
        lock (AuthNodeDictionary)
        {
            AuthNodeDictionary.TryGetValue(new AccountId(accountId), out var authNode);
            return authNode;
        }
    }

    public AuthNode AddAuthNode(LoginSessionData sd)
    {
        var authNode = new AuthNode(
            AccountId: sd.AccountId,
            LoginId1: sd.LoginId1,
            LoginId2: sd.LoginId2,
            Sex: sd.Sex,
            Ip: sd._socket.RemoteEndPoint?.GetHashCode() ?? 0,
            ClientType: sd.ClientType
        );
        lock (AuthNodeDictionary)
        {
            AuthNodeDictionary[new AccountId(authNode.AccountId)] = authNode;
        }

        return authNode;
    }

    public bool TryConsumeAuthNode(int accountId, int loginId1, int loginId2, char sex, out AuthNode? authNode)
    {
        lock (AuthNodeDictionary)
        {
            var key = new AccountId(accountId);
            if (AuthNodeDictionary.TryGetValue(key, out var candidate) &&
                candidate.LoginId1 == loginId1 &&
                candidate.LoginId2 == loginId2 &&
                candidate.Sex == sex)
            {
                AuthNodeDictionary.Remove(key);
                authNode = candidate;
                return true;
            }
        }

        authNode = default;
        return false;
    }

    public void RemoveAuthNode(int accountId)
    {
        lock (AuthNodeDictionary)
        {
            AuthNodeDictionary.Remove(new AccountId(accountId));
        }
    }

    public void Update(OnlineLoginData onlineLoginData)
    {
        lock (OnlineLoginDataDictionary)
        {
            OnlineLoginDataDictionary[onlineLoginData.AccountId] = onlineLoginData;
        }
    }

    // protected virtual so tests can observe disable calls without
    // needing a DB scope; production behavior is unchanged.
    protected virtual async Task UpdateAccountWebTokenEnabled(int accountId, bool enabled)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var loginRepository = scope.ServiceProvider.GetRequiredService<ILoginRepository>();
            var account = await loginRepository.GetByIdAsync(accountId);
            if (account != null)
            {
                account.WebAuthTokenEnabled = (short)(enabled ? 1 : 0);
                await loginRepository.UpdateAsync(account);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to update WebAuthTokenEnabled for account {AccountId} (enabled={Enabled})",
                accountId,
                enabled);
        }
    }
}
