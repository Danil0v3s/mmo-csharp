using System.Collections.Concurrent;
using Login.Server;

namespace Core.Server;

/// <summary>
/// Thread-safe registry for tracking connected character servers.
/// </summary>
public class CharServerRegistry : ICharServerRegistry
{
    private const int MaxServers = 5;

    private readonly CharServerData[] _charServers = new CharServerData[MaxServers];
    private readonly ConcurrentDictionary<int, string> _charServerSessions = new();
    private readonly object _lock = new();

    public CharServerRegistry()
    {
        for (int i = 0; i < _charServers.Length; i++)
        {
            _charServers[i] = new CharServerData
            {
                Name = string.Empty,
                SocketFd = -1,
                Ip = 0,
                Port = 0,
                Users = 0,
                Type = 0,
                New = 0
            };
        }
    }

    public void AddCharServer(int serverId, string serverName, uint serverIp, ushort serverPort, ushort serverType, ushort newServer)
    {
        if (serverId < 0 || serverId >= _charServers.Length)
            return;

        lock (_lock)
        {
            _charServers[serverId] = new CharServerData
            {
                Name = serverName,
                SocketFd = -1,
                Ip = serverIp,
                Port = serverPort,
                Users = 0,
                Type = serverType,
                New = newServer
            };

            _charServerSessions[serverId] = serverName;
        }
    }

    public void RemoveCharServer(int serverId)
    {
        if (serverId < 0 || serverId >= _charServers.Length)
            return;

        lock (_lock)
        {
            _charServerSessions.TryRemove(serverId, out _);
            _charServers[serverId] = new CharServerData
            {
                Name = string.Empty,
                SocketFd = -1,
                Ip = 0,
                Port = 0,
                Users = 0,
                Type = 0,
                New = 0
            };
        }
    }

    public CharServerData? GetCharServer(int serverId)
    {
        if (serverId < 0 || serverId >= _charServers.Length)
            return null;

        lock (_lock)
        {
            return _charServers[serverId];
        }
    }

    public IEnumerable<CharServerData> GetActiveCharServers()
    {
        lock (_lock)
        {
            return _charServers.Where(cs => !string.IsNullOrEmpty(cs.Name)).ToList();
        }
    }

    public IEnumerable<(int ServerId, CharServerData Data)> GetActiveCharServersWithIds()
    {
        lock (_lock)
        {
            var result = new List<(int, CharServerData)>();
            for (int i = 0; i < _charServers.Length; i++)
            {
                if (!string.IsNullOrEmpty(_charServers[i].Name))
                {
                    result.Add((i, _charServers[i]));
                }
            }
            return result;
        }
    }

    public bool HasActiveCharServers()
    {
        lock (_lock)
        {
            return _charServers.Any(cs => !string.IsNullOrEmpty(cs.Name));
        }
    }

    public void UpdateCharServerUserCount(int serverId, ushort userCount)
    {
        if (serverId < 0 || serverId >= _charServers.Length)
            return;

        lock (_lock)
        {
            _charServers[serverId] = _charServers[serverId] with { Users = userCount };
        }
    }

    public void UpdateCharServerAddress(int serverId, uint serverIp)
    {
        if (serverId < 0 || serverId >= _charServers.Length)
            return;

        lock (_lock)
        {
            _charServers[serverId] = _charServers[serverId] with { Ip = serverIp };
        }
    }

    public bool IsCharacterServer(int accountId)
    {
        return _charServerSessions.ContainsKey(accountId);
    }
}
