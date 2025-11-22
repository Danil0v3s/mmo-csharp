using Core.Timer;
using Login.Server.Model;
using Login.Server.Repository.Api;

namespace Login.Server.Repository.Impl;

internal class LoginDataRepository : ILoginDataRepository
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
            
            // loginrepository -> enable webtoken
            
            return onlineLoginData;
        }
    }

    public void RemoveOnlineUser(int accountId)
    {
        throw new NotImplementedException();
    }

    public AuthNode GetAuthNode(int accountId)
    {
        throw new NotImplementedException();
    }

    public AuthNode AddAuthNode(LoginSessionData sd, int ip)
    {
        throw new NotImplementedException();
    }

    public void RemoveAuthNode(int accountId)
    {
        throw new NotImplementedException();
    }

    public void Update(OnlineLoginData onlineLoginData)
    {
        _onlineLoginDataDictionary[onlineLoginData.AccountId] = onlineLoginData;
    }
}