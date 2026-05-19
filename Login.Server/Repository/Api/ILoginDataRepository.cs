using System.Net;
using Login.Server.Model;

namespace Login.Server.Repository.Api;


/// <summary>
/// In the lack of a better name, we call it LoginRepository.
/// It will store current logged in users, auth db etc
/// </summary>
public interface ILoginDataRepository
{
    public OnlineLoginData? GetOnlineUser(int accountId);
    public Task<OnlineLoginData> AddOnlineUser(int charServer, int accountId);
    public Task RemoveOnlineUser(int accountId);
    public Task<int> RemoveOnlineUsersByCharServer(int charServer);
    public void SetOnlineUserCharServer(int accountId, int charServer);
    void Update(OnlineLoginData onlineLoginData);

    /// <summary>
    /// Snapshot of all currently-online account ids and their char-server
    /// bindings. Defensive sweep input for
    /// <see cref="LoginServerImpl.SweepOrphanOnlineEntriesAsync"/> —
    /// mirrors rAthena's <c>login_online_data_cleanup</c> safety net
    /// (login.cpp:201) that runs every 10 min and removes entries whose
    /// char server is gone.
    /// </summary>
    IReadOnlyList<(int AccountId, int CharServer)> SnapshotOnlineEntries();
    
    public AuthNode? GetAuthNode(int accountId);
    public AuthNode AddAuthNode(LoginSessionData sd);
    public bool TryConsumeAuthNode(int accountId, int loginId1, int loginId2, char sex, out AuthNode? authNode);
    public void RemoveAuthNode(int accountId);
}
