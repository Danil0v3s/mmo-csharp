using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Mob;

/// <summary>
/// First-slice port of rAthena mob hard AI (mob.cpp:1741
/// <c>mob_ai_sub_hard</c>). Aggressive-only — looter, assist, slave,
/// MVP, RUDEATTACKED, and skill use plug in via subsequent slices.
///
/// Honors <c>MD_AGGRESSIVE</c> + <c>MD_CANATTACK</c> + <c>MD_CANMOVE</c>
/// modes as defined in <see cref="MobMode"/>; everything else stays
/// idle and goes back to the wander pass owned by <see cref="Spawn.IMobSpawnService"/>.
/// </summary>
public sealed class MobAiService : IMobAiService
{
    /// <summary>rAthena MIN_MOBTHINKTIME — minimum gap between AI ticks per mob.</summary>
    private const int MinThinkTimeMs = 100;

    private readonly IEntityRegistry _entities;
    private readonly IAttackService _attack;
    private readonly Dictionary<EntityId, long> _lastThink = new();

    public MobAiService(IEntityRegistry entities, IAttackService attack, ILogger<MobAiService> _)
    {
        _entities = entities;
        _attack = attack;
    }

    public void Tick(long nowTick)
    {
        // Iterate a snapshot; mobs can die mid-tick (via attack swings or
        // GM commands) which removes them from the registry.
        foreach (var entity in _entities.All().ToArray())
        {
            if (entity is not MobEntity mob) continue;
            if (mob.Hp <= 0) continue;
            if (mob.DbEntry == null) continue;

            // Throttle (rAthena last_thinktime).
            if (_lastThink.TryGetValue(mob.Id, out var last) && nowTick - last < MinThinkTimeMs) continue;
            _lastThink[mob.Id] = nowTick;

            var mode = mob.Stats.Mode;

            // Validate existing target — drop it if it's gone or unreachable.
            if (mob.Attack != null)
            {
                var current = _entities.Get(mob.Attack.TargetId);
                if (current == null || current.MapId != mob.MapId || !IsAlive(current))
                {
                    _attack.StopAttack(mob);
                }
                else
                {
                    continue; // Already engaged — let AttackService drive the swing/chase.
                }
            }

            if ((mode & MobMode.Aggressive) == 0 || (mode & MobMode.CanAttack) == 0)
                continue;

            // Scan PCs within view range. mob_db.View / db.range2 is the
            // aggro radius; mob.cpp:1758 uses range2 (default 14).
            var viewRange = mob.DbEntry.ChaseRange > 0
                ? mob.DbEntry.ChaseRange
                : Math.Max(1, mob.DbEntry.SkillRange);
            if (viewRange <= 0) viewRange = 12; // rAthena default mob view = 12.

            PlayerEntity? closest = null;
            int closestDist = int.MaxValue;
            foreach (var other in _entities.All())
            {
                if (other is not PlayerEntity pc) continue;
                if (pc.MapId != mob.MapId) continue;
                if (pc.Hp <= 0) continue;
                var dist = Math.Max(Math.Abs(pc.X - mob.X), Math.Abs(pc.Y - mob.Y));
                if (dist > viewRange) continue;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = pc;
                }
            }

            if (closest == null) continue;

            // Engage. AttackService validates range + drives chase/swings.
            _attack.StartAttack(mob, closest.Id, continuous: true);
        }

        // Periodically prune stale think entries — entries for mobs that
        // died never get rewritten so the dictionary would grow forever.
        if (_lastThink.Count > 0 && (nowTick & 0x1FFF) == 0) // every ~8s
        {
            var stale = _lastThink.Keys.Where(id => _entities.Get(id) is not MobEntity).ToList();
            foreach (var id in stale) _lastThink.Remove(id);
        }
    }

    private static bool IsAlive(Entity e) => e switch
    {
        PlayerEntity p => p.Hp > 0,
        MobEntity m => m.Hp > 0,
        _ => false,
    };
}
