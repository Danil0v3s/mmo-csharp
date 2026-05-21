using Map.Server.Entities;

namespace Map.Server.Mob;

/// <summary>
/// rAthena's spotted-log helpers (mob.cpp:99-145 — <c>mob_clean_spotted</c>,
/// <c>mob_add_spotted</c>, <c>mob_is_spotted</c>). Tracks which PCs have
/// seen this mob so the lazy AI can gate random-walk / idle-skill
/// ticks (mob.cpp:2418-2451 — bosses use the longer <c>boss_active_time</c>
/// window).
///
/// <para>State lives on <see cref="MobEntity.SpottedLog"/>. This class is a
/// stateless namespace of the three helpers; the visibility broadcaster
/// calls <see cref="Add"/> when a PC enters AOI and <see cref="Clean"/>
/// runs each lazy tick to evict char ids that no longer correspond to a
/// live player on the map.</para>
/// </summary>
public static class MobSpotted
{
    /// <summary>
    /// rAthena <c>mob_add_spotted</c> (mob.cpp:112). Records a PC's
    /// char id in the mob's spotted log. Caps at rAthena's
    /// <c>DAMAGELOG_SIZE = 30</c> — extra entries past the cap are
    /// dropped (Aegis behaviour: first-N wins, not LRU).
    /// </summary>
    public const int MaxSpotted = 30;

    public static void Add(MobEntity mob, int charId)
    {
        if (charId == 0) return;
        if (mob.SpottedLog.Count >= MaxSpotted) return;
        mob.SpottedLog.Add(charId);
    }

    /// <summary>
    /// rAthena <c>mob_clean_spotted</c> (mob.cpp:99). Drops char ids
    /// from the log that no longer match a live PC on the same map.
    /// Called once per lazy tick before the random-walk / idle-skill
    /// rolls (mob.cpp:2399).
    /// </summary>
    public static void Clean(MobEntity mob, IEntityRegistry entities)
    {
        if (mob.SpottedLog.Count == 0) return;

        // Build a quick set of live char ids on this map.
        // Falls back to "wipe everything" when the registry doesn't
        // surface PCs (e.g. tests with no PlayerEntity at all).
        var alive = new HashSet<int>();
        foreach (var e in entities.All())
        {
            if (e is not PlayerEntity p) continue;
            if (p.MapId != mob.MapId) continue;
            if (p.Hp <= 0) continue;
            alive.Add(p.CharacterId);
        }

        mob.SpottedLog.RemoveWhere(id => !alive.Contains(id));
    }

    /// <summary>rAthena <c>mob_is_spotted</c> (mob.cpp:135).</summary>
    public static bool IsSpotted(MobEntity mob) => mob.SpottedLog.Count > 0;
}
