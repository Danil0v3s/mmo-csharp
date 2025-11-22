using Login.Server.Model;

namespace Login.Server.Repository.Api;


/// <summary>
/// In the lack of a better name, we call it LoginRepository.
/// It will store current logged in users, auth db etc
/// </summary>
public interface ILoginDataRepository
{
    public OnlineLoginData GetOnlineUser(int accountId);
    public OnlineLoginData AddOnlineUser(int charServer, int accountId);
    public void RemoveOnlineUser(int accountId);
    
    public AuthNode GetAuthNode(int accountId);
    public AuthNode AddAuthNode(LoginSessionData sd, int ip);
    public void RemoveAuthNode(int accountId);
}