using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob.Conditions;
using Map.Server.Movement;
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
    private readonly IMovementService? _movement;
    private readonly IMobSkillCastService _mobSkillCast;
    private readonly IMobLooterService? _looter;
    private readonly IMobChangeTargetService _changeTarget;
    private readonly Random _rng;
    private readonly Dictionary<EntityId, long> _lastThink = new();

    public MobAiService(
        IEntityRegistry entities,
        IAttackService attack,
        ILogger<MobAiService> _,
        ISkillCastService? skillCast = null,
        MobSkillConditionRegistry? conditions = null,
        Random? rng = null,
        IMovementService? movement = null,
        IMobSkillCastService? mobSkillCast = null,
        IMobLooterService? looter = null,
        IMobChangeTargetService? changeTarget = null)
    {
        _entities = entities;
        _attack = attack;
        _movement = movement;
        _looter = looter;
        _changeTarget = changeTarget ?? new MobChangeTargetService();
        _rng = rng ?? Random.Shared;

        // Default to the standard evaluator set so existing tests don't
        // need to construct a registry by hand. Conditions feed the
        // mob_skill_use_id picker (T4.3) downstream.
        var defaultConditions = conditions ?? new MobSkillConditionRegistry(new IMobSkillConditionEvaluator[]
        {
            new AlwaysCondition(),
            new MyHpLessThanRateCondition(),
            new MyHpInRateCondition(),
            new RudeAttackedCondition(),
            new CloseAttackedCondition(),
            new LongRangeAttackedCondition(),
            new GroundAttackedCondition(),
            new SkillUsedCondition(),
            new CastTargetedCondition(),
            new DamagedGreaterCondition(),
            new AttackerCountGreaterCondition(),
            new AttackerCountGreaterEqCondition(),
            new SpawnCondition(),
            new SlaveLessThanCondition(),
            new SlaveLessEqCondition(),
            // T4.9a — self-status SC.
            new MyStatusOnCondition(),
            new MyStatusOffCondition(),
            // T4.9b — spatial / fake-cast.
            new MobNearbyGreaterCondition(),
            new TrickCastingCondition(),
            // T4.9e — slave-protect + alchemist summon.
            new MasterAttackedCondition(),
            new AlchemistCondition(),
        });

        // If the picker isn't injected, build one inline. This keeps the
        // existing test ctor signature intact (Skills + RNG + Conditions
        // is enough to wire a working picker without touching the test
        // bootstrap).
        _mobSkillCast = mobSkillCast ?? new MobSkillCastService(
            defaultConditions,
            new MobSkillTargetResolver(entities, rng: _rng),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MobSkillCastService>.Instance,
            skillCast,
            _rng);
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

            // T4.8 — lazy vs hard split (rAthena mob_ai_lazy / mob_ai_hard
            // at mob.cpp:2460 / 2467). Mobs without any PC in view fall
            // to the lazy path which only rolls idle-skill picks at a
            // low rate; mobs in active PC range go through the full
            // hard path below.
            if (!HasAnyPcInView(mob))
            {
                TickLazy(mob, nowTick);
                continue;
            }
            // T4.9c — every PC in view bumps the spotted log so the
            // lazy AI keeps animating even after the PCs walk out of
            // range (rAthena mob.cpp:1959, mob_add_spotted from the
            // per-PC hard-AI scan).
            SpotPcsInView(mob);

            // Validate existing target — drop it if it's gone or unreachable.
            if (mob.Attack != null)
            {
                var current = _entities.Get(mob.Attack.TargetId);
                if (current == null || current.MapId != mob.MapId || !IsAlive(current))
                {
                    _attack.StopAttack(mob);
                    // FSM transition — leaving combat → Idle (T4.8a).
                    MobFsm.TransitionTo(mob, MobSkillState.Idle);
                }
                else
                {
                    // Engaged — give the mob a chance to cast a skill instead
                    // of the basic swing. T4.3: route through the canonical
                    // IMobSkillCastService (rAthena mob.cpp:4275 mobskill_use).
                    MobFsm.TransitionTo(mob, MobSkillState.Berserk);
                    mob.TargetId = (int)current.Id.Value;
                    _mobSkillCast.TryUseSkill(mob, nowTick);
                    continue;
                }
            }

            // T4.9c — MD_LOOTER scan. Inserted between the slave/target
            // handling above and the aggressive scan below, mirroring
            // rAthena mob.cpp:2008-2129. Looter mobs walk to the
            // nearest floor item before considering aggro.
            if (_looter != null && _looter.IsLootEligible(mob))
            {
                var loot = _looter.FindNearestLoot(mob, IMobLooterService.DefaultLootRange);
                if (loot != null)
                {
                    var dist = Math.Max(Math.Abs(loot.X - mob.X), Math.Abs(loot.Y - mob.Y));
                    if (dist <= 1)
                    {
                        // Adjacent — pick it up this tick.
                        MobFsm.TransitionTo(mob, MobSkillState.Loot);
                        _looter.Collect(mob, loot);
                    }
                    else if (_movement != null)
                    {
                        // Walk toward the drop. Subsequent ticks will
                        // re-evaluate once the mob is adjacent.
                        MobFsm.TransitionTo(mob, MobSkillState.Loot);
                        _movement.TryStartWalk(mob, loot.X, loot.Y);
                    }
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
    /// T4.8 — has any live PC within view-range of the mob?
    /// Cheap O(N) scan; if profiling shows it as a hot path, the
    /// per-map PC bucket from IEntityRegistry can take over.
    /// rAthena equivalent: <c>map_getmapdata(md-&gt;m)-&gt;users &gt; 0</c>
    /// gate at <c>mob.cpp:2372</c>, but our test maps only host the
    /// mobs and PCs the test added so the proxy is "any PC in the
    /// mob's view range." Boss mobs (MD_STATUSIMMUNE) use a wider
    /// active radius (rAthena <c>battle_config.boss_active_time</c>) —
    /// that delta is a follow-up.
    /// </summary>
    private bool HasAnyPcInView(MobEntity mob)
    {
        var viewRange = mob.DbEntry?.ChaseRange > 0
            ? (short)mob.DbEntry.ChaseRange
            : (short)12;
        var nearby = _entities.ForEachInRange(
            mob.MapId, mob.X, mob.Y, viewRange, EntityType.Pc);
        foreach (var e in nearby)
        {
            if (e is PlayerEntity p && p.Hp > 0) return true;
        }
        return false;
    }

    /// <summary>
    /// rAthena <c>mob_add_spotted</c> (mob.cpp:112). Records every PC
    /// currently in view onto the mob's spotted log so the lazy AI
    /// keeps animating even after the PCs walk out of range. Called
    /// only on hard-AI ticks (lazy ticks prune via
    /// <see cref="MobSpotted.Clean"/>).
    /// </summary>
    private void SpotPcsInView(MobEntity mob)
    {
        var viewRange = mob.DbEntry?.ChaseRange > 0
            ? (short)mob.DbEntry.ChaseRange
            : (short)12;
        var nearby = _entities.ForEachInRange(
            mob.MapId, mob.X, mob.Y, viewRange, EntityType.Pc);
        foreach (var e in nearby)
        {
            if (e is PlayerEntity p && p.Hp > 0)
                MobSpotted.Add(mob, p.CharacterId);
        }
    }

    /// <summary>
    /// T4.8 + T4.9c — lazy AI path (rAthena <c>mob_ai_sub_lazy</c>
    /// <c>mob.cpp:2359</c>). Far-from-PC mobs only roll an idle-skill
    /// pick at the rAthena-default low rate (~5% of ticks for
    /// non-boss mobs) AND ONLY when the mob has been spotted by at
    /// least one PC (mob.cpp:2448 — <c>mob_is_spotted</c> gate).
    /// No target acquisition, no chase, no swing — just an
    /// occasional buff/transform/emote.
    /// </summary>
    private void TickLazy(MobEntity mob, long nowTick)
    {
        // rAthena mob.cpp:2399 — prune disconnected PC ids from the
        // spotted log before consulting it. We piggy-back on the lazy
        // tick because it's the same cadence rAthena uses.
        MobSpotted.Clean(mob, _entities);

        // rAthena mob.cpp:2448-2451 — only roll the idle-skill pick
        // when the mob has been spotted. Aegis 100%-fires for spotted
        // mobs; rAthena gates further on mob_nopc_idleskill_rate
        // (default 5/100). Boss mobs use boss_nopc_idleskill_rate.
        if (!mob.IsSpotted) return;
        if (_rng.Next(100) >= 5) return;

        // Use Idle FSM bucket so the picker only considers
        // MSS_IDLE / MSS_ANY rows.
        MobFsm.TransitionTo(mob, MobSkillState.Idle);
        _mobSkillCast.TryUseSkill(mob, nowTick);
    }

    /// <summary>
    /// rAthena <c>mob_damage</c> path (mob.cpp:1748): on each incoming hit
    /// the mob inspects whether the attacker is in melee reach. If not,
    /// <c>md->state.attacked_count</c> climbs. Once it crosses
    /// <c>battle.mob_rudeattacked_count</c> (default 2) the AI tries
    /// MSC_RUDEATTACKED — and if no skill matches, falls back to
    /// <c>unit_escape</c> (walk away). The counter clears on the next
    /// successful melee swing from this mob.
    /// </summary>
    public void NotifyAttacked(MobEntity mob, Entity attacker)
    {
        if (mob.Hp <= 0) return;

        // Attacker reachable? Use Chebyshev distance vs the mob's attack
        // range — a hit landing from within range isn't "rude," it's a
        // normal melee trade.
        var range = Math.Max(1, (int)mob.Stats.AttackRange);
        var dist = Math.Max(Math.Abs(attacker.X - mob.X), Math.Abs(attacker.Y - mob.Y));
        if (attacker.MapId == mob.MapId && dist <= range)
        {
            // In melee. Counter resets — rAthena clears attacked_count
            // when the mob lands a swing; we do it on the hit it receives
            // while in reach (functionally equivalent — same trip wire).
            mob.RudeAttackedCount = 0;
            return;
        }

        mob.RudeAttackedCount++;
        if (mob.RudeAttackedCount < RudeAttackedCondition.DefaultThreshold) return;

        // Crossed the threshold. T4.3: route through the canonical
        // IMobSkillCastService. First try the broader Berserk-state
        // picker, then a direct event-driven MSC_RUDEATTACKED trigger;
        // if neither fires, fall back to unit_escape.
        var now = Environment.TickCount64;
        mob.AttackedId = (int)attacker.Id.Value;
        // T4.9d — rAthena mob.cpp:1955 — when the AI is choosing
        // between current target and incoming attacker, the
        // mob_can_changetarget gate picks the winner. If allowed,
        // re-aim. Pure target-id mutation; the engage-on-Tick path
        // picks it up next pump.
        _changeTarget.TrySetTarget(mob, attacker);

        var fired = _mobSkillCast.TryUseSkill(mob, now)
                    || _mobSkillCast.NotifyEvent(mob, attacker, now, MobSkillCondition.RudeAttacked);
        if (!fired)
        {
            Escape(mob, attacker);
        }
        // Reset whether we cast or escaped — give the mob a fresh window
        // before we try again (rAthena: state.attacked_count = 0 after
        // mobskill_use returns true OR after unit_escape).
        mob.RudeAttackedCount = 0;
    }

    /// <summary>
    /// rAthena <c>mob_target</c> path (mob.cpp:1290). Public so the
    /// damage path (which sees every incoming hit, not just rude
    /// ones) can request a target switch and have the
    /// <see cref="IMobChangeTargetService"/> gate decide whether the
    /// mob honours it.
    /// </summary>
    public bool TryChangeTarget(MobEntity mob, Entity newTarget)
        => _changeTarget.TrySetTarget(mob, newTarget);

    /// <summary>
    /// rAthena <c>unit_escape</c> (unit.cpp:2240). Pick a cell roughly
    /// opposite the attacker direction and walk to it; clear the attack
    /// target so the mob doesn't immediately re-engage on the next tick.
    /// Skips silently if <see cref="IMovementService"/> isn't wired (most
    /// tests skip movement).
    /// </summary>
    private void Escape(MobEntity mob, Entity attacker)
    {
        _attack.StopAttack(mob);
        if (_movement == null) return;
        // Aim ~5 cells away along the (mob - attacker) vector.
        var dx = Math.Sign(mob.X - attacker.X);
        var dy = Math.Sign(mob.Y - attacker.Y);
        if (dx == 0 && dy == 0) dx = 1; // attacker exactly on top — bias east.
        var targetX = (short)Math.Clamp(mob.X + dx * 5, 0, short.MaxValue);
        var targetY = (short)Math.Clamp(mob.Y + dy * 5, 0, short.MaxValue);
        _movement.TryStartWalk(mob, targetX, targetY);
    }
}
