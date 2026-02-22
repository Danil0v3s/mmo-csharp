using System.Net;
using Core.Database.Repositories.Api;
using Core.Timer;
using Login.Server.Model;
using Login.Server.Repository.Api;

namespace Login.Server.Repository.Impl;

internal class LoginDataRepository(ILoginRepository loginRepository) : ILoginDataRepository
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

    public OnlineLoginData AddOnlineUser(int charServer, int accountId)
    {
        lock (OnlineLoginDataDictionary)
        {
            var accId = new AccountId(accountId);
            if (!OnlineLoginDataDictionary.TryGetValue(accId, out var onlineLoginData))
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
                onlineLoginData = onlineLoginData with { CharServer = charServer };

                if (onlineLoginData.WaitingDisconnect != Scheduler.InvalidTimer)
                {
                    Scheduler.Cancel(onlineLoginData.WaitingDisconnect);
                    onlineLoginData = onlineLoginData with { WaitingDisconnect = Scheduler.InvalidTimer };
                }
            }

            UpdateAccountWebTokenEnabled(accountId, true);

            return onlineLoginData;
        }
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

    public void RemoveOnlineUser(int accountId)
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

        UpdateAccountWebTokenEnabled(accountId, false);
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

    private async void UpdateAccountWebTokenEnabled(int accountId, bool enabled)
    {
        var account = await loginRepository.GetByIdAsync(accountId);
        if (account != null)
        {
            account.WebAuthTokenEnabled = (short)(enabled ? 1 : 0);
            await loginRepository.UpdateAsync(account);
        }
    }
}
