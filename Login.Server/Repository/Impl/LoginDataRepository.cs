using System.Net;
using Core.Database.Repositories.Api;
using Core.Timer;
using Login.Server.Model;
using Login.Server.Repository.Api;

namespace Login.Server.Repository.Impl;

internal class LoginDataRepository(ILoginRepository loginRepository) : ILoginDataRepository
{
    private Dictionary<AccountId, OnlineLoginData> _onlineLoginDataDictionary = new();
    private Dictionary<AccountId, AuthNode> _authNodeDictionary = new();

    public OnlineLoginData GetOnlineUser(int accountId)
    {
        lock (_onlineLoginDataDictionary)
        {
            return _onlineLoginDataDictionary[new AccountId(accountId)];
        }
    }

    public OnlineLoginData AddOnlineUser(int charServer, int accountId)
    {
        lock (_onlineLoginDataDictionary)
        {
            var accId = new AccountId(accountId);
            if (!_onlineLoginDataDictionary.TryGetValue(accId, out var onlineLoginData))
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

    public void RemoveOnlineUser(int accountId)
    {
        lock (_onlineLoginDataDictionary)
        {
            var accId = new AccountId(accountId);
            if (_onlineLoginDataDictionary.TryGetValue(accId, out var onlineLoginData))
            {
                if (onlineLoginData.WaitingDisconnect != Scheduler.InvalidTimer)
                {
                    Scheduler.Cancel(onlineLoginData.WaitingDisconnect);
                }

                _onlineLoginDataDictionary.Remove(accId);
            }
        }

        UpdateAccountWebTokenEnabled(accountId, false);
    }

    public AuthNode GetAuthNode(int accountId)
    {
        lock (_authNodeDictionary)
        {
            return _authNodeDictionary[new AccountId(accountId)];
        }
    }

    public AuthNode AddAuthNode(LoginSessionData sd)
    {
        var authNode = new AuthNode(
            AccountId: sd.AccountId,
            LoginId1: sd.LoginId1,
            LoginId2: sd.LoginId2,
            Sex: sd.Sex,
            Ip: sd._socket.RemoteEndPoint.GetHashCode(), // todo what's this?
            ClientType: sd.ClientType
        );
        lock (_authNodeDictionary)
        {
            _authNodeDictionary.Add(new AccountId(authNode.AccountId), authNode);
        }

        return authNode;
    }

    public void RemoveAuthNode(int accountId)
    {
        lock (_authNodeDictionary)
        {
            _authNodeDictionary.Remove(new AccountId(accountId));
        }
    }

    public void Update(OnlineLoginData onlineLoginData)
    {
        lock (_onlineLoginDataDictionary)
        {
            _onlineLoginDataDictionary[onlineLoginData.AccountId] = onlineLoginData;
        }
    }

    private async void UpdateAccountWebTokenEnabled(int accountId, bool enabled)
    {
        var account = await loginRepository.GetByIdAsync(accountId);
        if (account != null)
        {
            account.WebAuthTokenEnabled = 0;
            await loginRepository.UpdateAsync(account);
        }
    }
}