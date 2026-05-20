using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Map.Server.Scripting.MapReg;

/// <summary>
/// Default <see cref="IMapRegService"/>. In-memory map of int → long
/// and int → string. The SQL persistence pass (load / flush) is
/// data-pending on a `mapreg` repository — the entry points are
/// here so script-engine writes don't drift into PlayerEntity.
/// </summary>
public sealed class MapRegService : IMapRegService
{
    private readonly ConcurrentDictionary<int, long> _ints = new();
    private readonly ConcurrentDictionary<int, string> _strs = new();
    private readonly ILogger<MapRegService> _logger;

    public MapRegService(ILogger<MapRegService> logger) => _logger = logger;

    public long ReadReg(int key) => _ints.TryGetValue(key, out var v) ? v : 0;
    public string? ReadRegStr(int key) => _strs.TryGetValue(key, out var v) ? v : null;

    public bool SetReg(int key, long value)
    {
        if (value == 0) { _ints.TryRemove(key, out _); return true; }
        _ints[key] = value;
        return true;
    }

    public bool SetRegStr(int key, string? value)
    {
        if (string.IsNullOrEmpty(value)) { _strs.TryRemove(key, out _); return true; }
        _strs[key] = value;
        return true;
    }

    public int DestroyReg(int key)
    {
        var i = _ints.TryRemove(key, out _) ? 1 : 0;
        var s = _strs.TryRemove(key, out _) ? 1 : 0;
        return i + s;
    }

    public void Init() { /* SQL load data-pending */ }
    public void Final() { /* SQL flush data-pending */ }
    public void Reload() { _ints.Clear(); _strs.Clear(); Init(); }
    public bool ConfigRead(string configPath) => true;
}
