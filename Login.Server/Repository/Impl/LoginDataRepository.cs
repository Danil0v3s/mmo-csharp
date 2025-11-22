using Login.Server.Model;
using Login.Server.Repository.Api;
using Timer = Login.Server.Model.Timer;

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
            if (!_onlineLoginDataDictionary.TryGetValue(new AccountId(accountId), out var onlineLoginData))
            {
                onlineLoginData = new OnlineLoginData(
                    CharServer: charServer,
                    AccountId: accountId,
                    WaitingDisconnect: Timer.INVALID_TIMER,
                    VipTimeoutTid: Timer.INVALID_TIMER
                );
            }
            else
            {
                onlineLoginData = onlineLoginData with { CharServer = charServer };

                if (onlineLoginData.WaitingDisconnect != Timer.INVALID_TIMER)
                {
                    onlineLoginData = onlineLoginData with { WaitingDisconnect = Timer.INVALID_TIMER }; 
                } 
            }
            
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
}