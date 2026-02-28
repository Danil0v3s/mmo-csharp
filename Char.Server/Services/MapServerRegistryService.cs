using System.Collections.Concurrent;

namespace Char.Server.Services;

public class MapServerRegistryService : IMapServerRegistryService
{
    private readonly ConcurrentDictionary<int, string[]> _mapsByServer = new();
    private readonly ConcurrentDictionary<int, int> _userCountsByServer = new();
    private readonly ConcurrentDictionary<int, (uint Ip, uint Port)> _addressesByServer = new();

    public int RegisterMaps(int mapServerId, IEnumerable<string> maps)
    {
        if (mapServerId <= 0)
        {
            return 0;
        }

        var normalized = maps
            .Where(map => !string.IsNullOrWhiteSpace(map))
            .Select(map => map.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _mapsByServer[mapServerId] = normalized;
        return normalized.Length;
    }

    public void SetUserCount(int mapServerId, int userCount)
    {
        if (mapServerId <= 0 || userCount < 0)
        {
            return;
        }

        _userCountsByServer[mapServerId] = userCount;
    }

    public bool TryGetUserCount(int mapServerId, out int userCount)
    {
        if (mapServerId <= 0)
        {
            userCount = 0;
            return false;
        }

        return _userCountsByServer.TryGetValue(mapServerId, out userCount);
    }

    public void SetAddress(int mapServerId, uint ip, uint port)
    {
        if (mapServerId <= 0)
        {
            return;
        }

        _addressesByServer[mapServerId] = (ip, port);
    }

    public bool HasServer(int mapServerId)
    {
        if (mapServerId <= 0)
        {
            return false;
        }

        return _mapsByServer.ContainsKey(mapServerId) ||
               _userCountsByServer.ContainsKey(mapServerId) ||
               _addressesByServer.ContainsKey(mapServerId);
    }

    public bool ContainsMap(string mapName)
    {
        if (string.IsNullOrWhiteSpace(mapName))
        {
            return false;
        }

        return _mapsByServer.Values.Any(maps => maps.Contains(mapName.Trim(), StringComparer.OrdinalIgnoreCase));
    }
}
