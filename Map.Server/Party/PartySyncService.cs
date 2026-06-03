using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Party;

/// <summary>
/// Default <see cref="IPartySyncService"/>. rAthena <c>party_send_xy_timer</c> + <c>clif_party_hp</c>:
/// every ~1 s, for each online party member whose cell or HP changed since the last sync, broadcast
/// the new position + HP to their same-map party members (PARTY_AREA_WOS). Change-tracking avoids
/// flooding the wire when nothing moved.
/// </summary>
public sealed class PartySyncService : IPartySyncService
{
    private const long IntervalMs = 1000; // rAthena battle_config.party_update_interval default

    private readonly IEntityRegistry _entities;
    private readonly IPartyMapService _partyMap;
    private readonly ISessionManagerAccessor _sessions;
    private readonly ILogger<PartySyncService> _logger;

    private readonly Dictionary<int, (short X, short Y)> _lastXy = new();
    private readonly Dictionary<int, int> _lastHp = new();
    private long _nextTick;

    public PartySyncService(IEntityRegistry entities, IPartyMapService partyMap,
        ISessionManagerAccessor sessions, ILogger<PartySyncService> logger)
    {
        _entities = entities;
        _partyMap = partyMap;
        _sessions = sessions;
        _logger = logger;
    }

    public void Tick(long nowTick)
    {
        if (nowTick < _nextTick) return;
        _nextTick = nowTick + IntervalMs;

        foreach (var e in _entities.All())
        {
            if (e is not PlayerEntity pc || pc.PartyId == 0) continue;

            var posChanged = !_lastXy.TryGetValue(pc.CharacterId, out var lastXy) || lastXy.X != pc.X || lastXy.Y != pc.Y;
            var hpChanged = !_lastHp.TryGetValue(pc.CharacterId, out var lastHp) || lastHp != pc.Hp;
            if (!posChanged && !hpChanged) continue;

            _lastXy[pc.CharacterId] = (pc.X, pc.Y);
            _lastHp[pc.CharacterId] = pc.Hp;

            var pos = posChanged ? new ZC_NOTIFY_POSITION_TO_GROUPM { AccountId = (uint)pc.AccountId, X = pc.X, Y = pc.Y } : null;
            var hp = hpChanged ? new ZC_NOTIFY_HP_TO_GROUPM { AccountId = (uint)pc.AccountId, Hp = pc.Hp, MaxHp = pc.MaxHp } : null;

            // Broadcast to the member's same-map party members (excluding self — PARTY_AREA_WOS).
            _partyMap.ForEachOnSameMap(pc, member =>
            {
                var session = _sessions.GetByEntityId(member.Id);
                if (session == null) return;
                if (pos != null) session.EnqueuePacket(pos);
                if (hp != null) session.EnqueuePacket(hp);
            }, includeSelf: false);
        }
    }
}
