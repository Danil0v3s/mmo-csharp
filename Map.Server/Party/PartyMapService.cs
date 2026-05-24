using Map.Server.Entities;

namespace Map.Server.Party;

/// <summary>
/// Default <see cref="IPartyMapService"/>. Walks
/// <see cref="IEntityRegistry"/> filtered by <see cref="PlayerEntity.PartyId"/>,
/// <see cref="Entity.MapId"/>, and HP&gt;0. Matches the eligibility set
/// rAthena <c>party_foreachsamemap</c> applies before invoking its
/// callback.
/// </summary>
public sealed class PartyMapService : IPartyMapService
{
    private readonly IEntityRegistry _entities;

    public PartyMapService(IEntityRegistry entities) => _entities = entities;

    public int ForEachOnSameMap(PlayerEntity origin, Action<PlayerEntity> callback, bool includeSelf = true)
        => ForEachOnSameMapInRange(origin, -1, callback, includeSelf);

    public int ForEachOnSameMapInRange(PlayerEntity origin, short range, Action<PlayerEntity> callback, bool includeSelf = true)
    {
        if (callback == null) return 0;

        // Solo or party-less: treat origin as a single-member set.
        if (origin.PartyId <= 0)
        {
            if (!includeSelf || origin.Hp <= 0) return 0;
            callback(origin);
            return 1;
        }

        var count = 0;
        foreach (var e in _entities.All())
        {
            if (e is not PlayerEntity pc) continue;
            if (pc.PartyId != origin.PartyId) continue;
            if (pc.MapId != origin.MapId) continue;
            if (pc.Hp <= 0) continue;
            if (!includeSelf && pc.Id.Value == origin.Id.Value) continue;
            if (range >= 0)
            {
                var dx = Math.Abs(pc.X - origin.X);
                var dy = Math.Abs(pc.Y - origin.Y);
                if (dx > range || dy > range) continue;
            }
            callback(pc);
            count++;
        }
        return count;
    }
}
