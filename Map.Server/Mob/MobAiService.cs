using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob.Conditions;
using Map.Server.Skills;
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
    private readonly ISkillCastService? _skillCast;
    private readonly MobSkillConditionRegistry _conditions;
    private readonly Random _rng;
    private readonly Dictionary<EntityId, long> _lastThink = new();
    /// <summary>Per-mob, per-skill cooldown anchor (Environment.TickCount64).</summary>
    private readonly Dictionary<(EntityId mobId, int skillIndex), long> _skillDelay = new();

    public MobAiService(
        IEntityRegistry entities,
        IAttackService attack,
        ILogger<MobAiService> _,
        ISkillCastService? skillCast = null,
        MobSkillConditionRegistry? conditions = null,
        Random? rng = null)
    {
        _entities = entities;
        _attack = attack;
        _skillCast = skillCast;
        // Default to the standard evaluator set so existing tests don't
        // need to construct a registry by hand.
        _conditions = conditions ?? new MobSkillConditionRegistry(new IMobSkillConditionEvaluator[]
        {
            new AlwaysCondition(),
            new MyHpLessThanRateCondition(),
        });
        _rng = rng ?? Random.Shared;
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
                    // Engaged — give the mob a chance to cast a skill instead
                    // of the basic swing. mobskill_use (mob.cpp:3924) MSS_BERSERK
                    // path: a skill the mob has assigned can trigger between
                    // normal attacks once its delay elapsed and the roll passes.
                    TryUseMobSkill(mob, current, MobSkillState.Berserk, nowTick);
                    continue;
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

    /// <summary>
    /// Port of rAthena <c>mobskill_use</c> condition loop (mob.cpp:3924),
    /// MS3 first slice — evaluates Always / MyHpLessThanRate triggers.
    /// Returns true if a skill was cast (the caller should usually skip
    /// the basic-swing path for this tick).
    /// </summary>
    private bool TryUseMobSkill(MobEntity mob, Entity target, MobSkillState state, long nowTick)
    {
        if (_skillCast == null) return false;
        if (mob.DbEntry == null || mob.DbEntry.Skills.Count == 0) return false;
        if ((mob.Stats.Mode & MobMode.NoCast) != 0) return false;

        // rAthena: random starting index when MOB_AI flag 0x100 is set.
        var start = _rng.Next(mob.DbEntry.Skills.Count);
        for (var n = 0; n < mob.DbEntry.Skills.Count; n++)
        {
            var i = (start + n) % mob.DbEntry.Skills.Count;
            var entry = mob.DbEntry.Skills[i];

            // State match — only Berserk skills fire while attacking.
            if (entry.State != MobSkillState.Any && entry.State != state) continue;

            // Per-mob, per-skill cooldown.
            var key = (mob.Id, i);
            if (_skillDelay.TryGetValue(key, out var readyAt) && readyAt > nowTick) continue;

            // Condition evaluation — strategy dispatch via
            // MobSkillConditionRegistry. New rAthena conditions (slave
            // counts, master-attacked, ground-attacked, etc.) ship as
            // a new IMobSkillConditionEvaluator class.
            var evaluator = _conditions.Get(entry.Condition);
            if (evaluator == null || !evaluator.IsMet(mob, target, entry)) continue;

            // Permillage gate (out of 10,000).
            if (_rng.Next(10_000) >= entry.Permillage) continue;

            var castResult = _skillCast.StartCast(mob, target.Id, entry.SkillId, entry.SkillLevel);
            if (castResult == SkillCastResult.Started)
            {
                _skillDelay[key] = nowTick + Math.Max(1, entry.DelayMs);
                return true;
            }
        }
        return false;
    }
}
