namespace Char.Server.Services;

public interface IMapServerRegistryService
{
    int RegisterMaps(int mapServerId, IEnumerable<string> maps);
    void SetUserCount(int mapServerId, int userCount);
    bool TryGetUserCount(int mapServerId, out int userCount);
    void SetAddress(int mapServerId, uint ip, uint port);
    bool HasServer(int mapServerId);
    bool ContainsMap(string mapName);
}
