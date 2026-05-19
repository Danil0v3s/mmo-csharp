using System.Collections.Concurrent;
using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// First-slice <see cref="IPlayerTimerService"/>. Until the script
/// host needs them in anger, timers are tracked in-memory with a
/// growing int id. Dispatch is documented as the script engine's
/// responsibility on fire — we just hold the (pc, event, fire-tick)
/// triples.
/// </summary>
public sealed class PlayerTimerService : IPlayerTimerService
{
    private readonly ConcurrentDictionary<int, Entry> _timers = new();
    private int _nextId;
    private readonly ILogger<PlayerTimerService> _logger;
    public PlayerTimerService(ILogger<PlayerTimerService> logger) => _logger = logger;

    public int Add(PlayerEntity pc, string eventName, int ms)
    {
        var id = Interlocked.Increment(ref _nextId);
        _timers[id] = new Entry(pc.CharacterId, eventName, Environment.TickCount64 + ms);
        return id;
    }

    public void Delete(PlayerEntity pc, int timerId) => _timers.TryRemove(timerId, out _);

    public void ClearAll(PlayerEntity pc)
    {
        foreach (var (id, e) in _timers)
        {
            if (e.CharId == pc.CharacterId) _timers.TryRemove(id, out _);
        }
    }

    public int AddCountToAll(PlayerEntity pc, string eventName, int ms)
    {
        var n = 0;
        foreach (var (_, e) in _timers)
        {
            if (e.CharId == pc.CharacterId && e.EventName == eventName)
            {
                e.FireTick += ms;
                n++;
            }
        }
        return n;
    }

    private sealed class Entry
    {
        public int CharId;
        public string EventName;
        public long FireTick;
        public Entry(int charId, string eventName, long fireTick)
        {
            CharId = charId;
            EventName = eventName;
            FireTick = fireTick;
        }
    }
}

/// <summary>
/// In-memory <see cref="IPlayerBgQueueTimerService"/>. BG queue is
/// not yet implemented — when battleground_db ports, this service
/// will fire the queue-expiry event into the BG dispatcher. For now
/// it accepts + cancels timer requests so callers can be wired.
/// </summary>
public sealed class PlayerBgQueueTimerService : IPlayerBgQueueTimerService
{
    private readonly ConcurrentDictionary<int, long> _expiries = new();
    public void Set(PlayerEntity pc, int ms) => _expiries[pc.CharacterId] = Environment.TickCount64 + ms;
    public void Delete(PlayerEntity pc) => _expiries.TryRemove(pc.CharacterId, out _);
}

/// <summary>
/// In-memory <see cref="IPlayerHateService"/>. Hate state on rAthena
/// lives on the session — we mirror with a per-character map keyed
/// by slot. Persistence to mmo_charstatus.hate.target[] lands when
/// the field surfaces on CharacterDataResponse.
/// </summary>
public sealed class PlayerHateService : IPlayerHateService
{
    private readonly ConcurrentDictionary<(int CharId, int Slot), int> _hate = new();

    public bool SetHateMob(PlayerEntity pc, int position, int mobClassId)
    {
        if (position < 0 || position > 2) return false;
        _hate[(pc.CharacterId, position)] = mobClassId;
        return true;
    }

    public int GetHateMob(PlayerEntity pc, int position)
        => _hate.TryGetValue((pc.CharacterId, position), out var c) ? c : 0;
}

/// <summary>
/// First-slice <see cref="IPlayerJailService"/>. Tracks the jailed
/// flag + release time. Actual warp to the jail map (sec_pri) lives
/// in the warp service hook; the timer service can call <see cref="Unjail"/>
/// when the duration elapses.
/// </summary>
public sealed class PlayerJailService : IPlayerJailService
{
    private readonly ConcurrentDictionary<int, long> _jailedUntil = new();
    private readonly ILogger<PlayerJailService> _logger;
    public PlayerJailService(ILogger<PlayerJailService> logger) => _logger = logger;

    public bool Jail(PlayerEntity pc, int minutes, string? reason = null)
    {
        var ends = minutes <= 0 ? long.MaxValue : DateTimeOffset.UtcNow.ToUnixTimeSeconds() + minutes * 60;
        _jailedUntil[pc.CharacterId] = ends;
        _logger.LogInformation("pc_jail: char {Char} for {Min}m reason={Reason}", pc.CharacterId, minutes, reason);
        return true;
    }

    public bool Unjail(PlayerEntity pc) => _jailedUntil.TryRemove(pc.CharacterId, out _);

    public bool IsJailed(PlayerEntity pc)
        => _jailedUntil.TryGetValue(pc.CharacterId, out var ends)
           && ends > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
