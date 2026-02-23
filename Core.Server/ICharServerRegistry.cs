using Login.Server;

namespace Core.Server;

/// <summary>
/// Registry for tracking connected character servers.
/// Pure data management - no IPC or server lifecycle dependencies.
/// </summary>
public interface ICharServerRegistry
{
    CharServerData? GetCharServer(int serverId);
    IEnumerable<CharServerData> GetActiveCharServers();
    IEnumerable<(int ServerId, CharServerData Data)> GetActiveCharServersWithIds();
    bool HasActiveCharServers();
    void AddCharServer(int serverId, string serverName, uint serverIp, ushort serverPort, ushort serverType, ushort newServer);
    void RemoveCharServer(int serverId);
    void UpdateCharServerUserCount(int serverId, ushort userCount);
    void UpdateCharServerAddress(int serverId, uint serverIp);
    bool IsCharacterServer(int accountId);
}
