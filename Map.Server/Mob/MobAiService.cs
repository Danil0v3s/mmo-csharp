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

    /// <summary>rAthena <c>battle_config.mob_active_time</c> / <c>boss_active_time</c> (battle.cpp
    /// default 5000 each). After the last PC leaves a mob's view, the mob keeps running its hard AI
    /// (full skill/target/chase logic) for this long — bosses (<c>MD_STATUSIMMUNE</c>) use the boss
    /// value so they can be tuned to stay active longer than trash mobs. AI-BOSS-ACTIVE-HP.</summary>
    private const long MobActiveTimeMs = 5000;
    private const long BossActiveTimeMs = 5000;

    private readonly IEntityRegistry _entities;
    private readonly IAttackService _attack;
    private readonly IMovementService? _movement;
    private readonly IMobSkillCastService _mobSkillCast;
    private readonly IMobLooterService? _looter;
    private readonly IMobChangeTargetService _changeTarget;
    private readonly IMobRandomWalkService? _wander;
    private readonly IStatusChangeService? _sc;
    // MOBAI-04 — line-of-sight gate for the aggressive scan (rAthena path_search_long /
    // status_check_visible). Null in tests → the scan degrades to distance-only (no LOS gate).
    private readonly Map.Server.Pathing.IPathService? _paths;
    // MOBAI-01 — slave→master coupling pass + the kill seam for a slave whose master is gone.
    private readonly Slaves.ISlaveMobService _slaves;
    private readonly Map.Server.Spawn.IMobDeathSink? _death;
    private readonly Random _rng;
    private readonly Dictionary<EntityId, long> _lastThink = new();
    // AI-BOSS-ACTIVE-HP — rAthena md->last_pcneartime: the tick a PC was last in this mob's view.
    private readonly Dictionary<EntityId, long> _lastPcNear = new();

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
        IMobChangeTargetService? changeTarget = null,
        IMobRandomWalkService? wander = null,
        IStatusChangeService? sc = null,
        Map.Server.Pathing.IPathService? paths = null,
        Slaves.ISlaveMobService? slaves = null,
        Map.Server.Spawn.IMobDeathSink? death = null)
    {
        _entities = entities;
        _attack = attack;
        _movement = movement;
        _looter = looter;
        // MOBAI-03 — pass the registry so the default service can run the changechase / random-enemy
        // scans (TryChangeChase / PickRandomEnemy use ForEachInRange).
        _changeTarget = changeTarget ?? new MobChangeTargetService(entities);
        _wander = wander;
        _sc = sc;
        _paths = paths;
        // MOBAI-01 — default to a SlaveMobService over the registry so the existing test ctor (which
        // doesn't pass one) keeps a working slave pass; production injects the DI singleton.
        _slaves = slaves ?? new Slaves.SlaveMobService(entities, sc, movement);
        _death = death;
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
            // MOBAI-01 — a mob with a master always runs the hard path (it stays on rAthena's
            // active list) so the slave-coupling pass follows its master even when the master is
            // kited away from any PC.
            var pcInView = HasAnyPcInView(mob);
            // AI-BOSS-ACTIVE-HP — record the last tick a PC was in view (rAthena md->last_pcneartime).
            if (pcInView) _lastPcNear[mob.Id] = nowTick;

            if (ShouldRunLazy(mob, pcInView, nowTick))
            {
                TickLazy(mob, nowTick);
                continue;
            }
            // T4.9c — every PC in view bumps the spotted log so the
            // lazy AI keeps animating even after the PCs walk out of
            // range (rAthena mob.cpp:1959, mob_add_spotted from the
            // per-PC hard-AI scan).
            if (pcInView) SpotPcsInView(mob);

            // T5.1d — rAthena mob.cpp:1864 OPT1 / SCF_MOBLOSETARGET
            // gate. If the mob is under an immobilizing SC (Stone /
            // Freeze / Stun / Sleep — but NOT Stonewait or Burning)
            // it drops its target ids and bails. The SCF flag scan
            // is documented-pending until status_yml exposes the
            // per-SC SCF_MOBLOSETARGET flag; the four canonical OPT1
            // SCs cover the common case.
            if (HasMobLoseTargetSc(mob))
            {
                _attack.StopAttack(mob);
                mob.TargetId = 0;
                mob.AttackedId = 0;
                MobFsm.TransitionTo(mob, MobSkillState.Idle);
                continue;
            }

            // Wave 66 / Track B — mob_ai_sub_hard_attacktimer post-swing
            // re-entry (mob.cpp:2095). After a mob swings, its
            // AttackState.AttackableTick gets pushed forward by adelay,
            // and the next re-think is gated on that — the mob doesn't
            // re-pick a target / wander while still in mid-swing.
            // Pre-Wave-66 this gate lived only inside AttackService.Tick;
            // pulling it into MobAiService.Tick prevents stale target
            // re-evaluation during the swing's amotion window.
            if (mob.Attack != null && mob.Attack.AttackableTick > nowTick)
            {
                continue;
            }

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

            // MOBAI-01 — slave→master coupling pass (rAthena mob_ai_sub_hard_slavemob, mob.cpp:1857):
            // runs after target validation, before the looter/aggressive scans. A slave follows its
            // master + inherits its target (Handled → skip the rest of this tick); a slave whose
            // master is gone dies with it; an un-handled aggressive slave still re-scans for aggro.
            bool slaveLostTarget = false;
            if (mob.MasterId != null)
            {
                switch (_slaves.TickSlave(mob, nowTick))
                {
                    case Slaves.SlaveTickResult.MasterGone:
                        // rAthena status_kill(slave) — no exp/loot credit (null last-hitter).
                        mob.Hp = 0;
                        _death?.KillMob(mob.Id, lastHitter: null);
                        continue;
                    case Slaves.SlaveTickResult.Handled:
                        // If the slave inherited a target this tick (TargetId set, not yet engaged),
                        // start the attack so it chases + swings — rAthena's md->target_id drives
                        // unit_attack on the next think; we engage it now. A pure follow (TargetId 0)
                        // just keeps walking.
                        if (mob.TargetId != 0 && mob.Attack == null)
                            _attack.StartAttack(mob, new EntityId(mob.TargetId), continuous: true);
                        continue;
                    case Slaves.SlaveTickResult.Continue:
                        slaveLostTarget = true; // rAthena: force the aggro scan even if it had a target
                        break;
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

            // mob_db.View / db.range2 aggro radius (mob.cpp:1758, default 14) — shared by the
            // aggressive scan and the MOBAI-03 changechase reach below.
            var viewRange = mob.DbEntry.ChaseRange > 0
                ? mob.DbEntry.ChaseRange
                : Math.Max(1, mob.DbEntry.SkillRange);
            if (viewRange <= 0) viewRange = 12; // rAthena default mob view = 12.

            // rAthena mob.cpp:1870 — the active scan runs when the mob is aggressive OR a slave that
            // had no master action this tick (slave_lost_target), so a slave joins its master's fight.
            if ((mode & MobMode.CanAttack) != 0 && ((mode & MobMode.Aggressive) != 0 || slaveLostTarget))
            {
                // MOBAI-04 — cell-grid range query (rAthena map_foreachinallrange over view_range),
                // replacing the full-registry `_entities.All()` walk. Same PC + alive + distance filter.
                PlayerEntity? closest = null;
                int closestDist = int.MaxValue;
                foreach (var other in _entities.ForEachInRange(mob.MapId, mob.X, mob.Y, (short)viewRange, EntityType.Pc))
                {
                    if (other is not PlayerEntity pc) continue;
                    if (pc.Hp <= 0) continue;
                    // MOBAI-03 — MD_TARGETWEAK (mob.cpp:1309): skip a PC unless it is at least 5
                    // levels below the mob (`status_get_lv(bl) >= md->level-5` → skip).
                    if ((mode & MobMode.TargetWeak) != 0 && pc.Level >= mob.Level - 5) continue;
                    var dist = Math.Max(Math.Abs(pc.X - mob.X), Math.Abs(pc.Y - mob.Y));
                    if (dist > viewRange) continue;
                    if (dist >= closestDist) continue;
                    // MOBAI-04 — line-of-sight gate (rAthena mob_ai_sub_hard_activesearch's
                    // status_check_skilluse / ACTIVEPATHSEARCH path_search): skip a wall-blocked PC.
                    // With no path service (tests) LOS is treated as clear — distance-only fallback.
                    if (_paths != null && !_paths.PathSearchLong(mob.MapId, mob.X, mob.Y, pc.X, pc.Y))
                        continue;
                    closestDist = dist;
                    closest = pc;
                }

                if (closest == null)
                {
                    // T4.9f — no enemy in view → roll random walk (rAthena mob.cpp:2059-2067).
                    _wander?.TryWander(mob, nowTick);
                    continue;
                }

                // MOBAI-03 — MD_RANDOMTARGET (mob.cpp:1993): swing ONCE at the current target then
                // re-aim at a RANDOM in-range enemy (min(view, attack range)); the non-random path
                // continuous-attacks. Engage the previously re-aimed target if still valid so the
                // target hops between swings; otherwise the freshly-scanned closest.
                if ((mode & MobMode.RandomTarget) != 0)
                {
                    Entity engage = closest;
                    if (mob.TargetId != 0 && _entities.Get(new EntityId(mob.TargetId)) is { } prev
                        && IsAlive(prev) && prev.MapId == mob.MapId)
                        engage = prev;
                    _attack.StartAttack(mob, engage.Id, continuous: false);
                    var reaimRange = (short)Math.Min(viewRange, Math.Max(1, (int)mob.Stats.AttackRange));
                    var next = _changeTarget.PickRandomEnemy(mob, reaimRange, _rng);
                    mob.TargetId = next != null ? (int)next.Id.Value : (int)engage.Id.Value;
                }
                else
                {
                    // Engage. AttackService validates range + drives chase/swings.
                    _attack.StartAttack(mob, closest.Id, continuous: true);
                }
            }
            // MOBAI-07 — MD_CHANGECHASE (mob.cpp:1882 → mob_ai_sub_hard_changechase, mob.cpp:1348): a
            // chasing mob (RUSH/FOLLOW) switches to an enemy already in its melee reach. rAthena sets
            // this target DIRECTLY (`md->target_id = bl->id`) — it does NOT run `mob_can_changetarget`,
            // because the in-reach-while-chasing case is its own dedicated switch. `TryChangeChase`
            // already applies rAthena's changechase gates (is-enemy + in-rhw-range). Exclusive `else if`
            // with the aggressive scan (a mob that ran the active search does not also changechase).
            else if ((mode & MobMode.ChangeChase) != 0
                     && (mob.SkillState == MobSkillState.Rush || mob.SkillState == MobSkillState.Follow))
            {
                var reach = (short)Math.Min(viewRange, Math.Max(1, (int)mob.Stats.AttackRange));
                var newTarget = _changeTarget.TryChangeChase(mob, reach, t => Perceives(mob, t));
                if (newTarget != null)
                    mob.TargetId = (int)newTarget.Id.Value;
            }
        }

        // Periodically prune stale think entries — entries for mobs that
        // died never get rewritten so the dictionary would grow forever.
        if (_lastThink.Count > 0 && (nowTick & 0x1FFF) == 0) // every ~8s
        {
            var stale = _lastThink.Keys.Where(id => _entities.Get(id) is not MobEntity).ToList();
            foreach (var id in stale) { _lastThink.Remove(id); _lastPcNear.Remove(id); }
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
    /// T5.1d / Wave 63 — rAthena mob.cpp:1864 OPT1 + SCF_MOBLOSETARGET gate.
    /// True iff the mob currently has any OPT1 CC (Stone / Freeze / Stun /
    /// Sleep) active OR an SC whose status.yml flag row carries
    /// <c>MobLoseTarget: true</c>. Stonewait (petrify charge-up) and
    /// Burning are explicitly excluded — mobs keep their target through
    /// both. The MOBLOSETARGET set is sourced from
    /// <c>rathena/db/re/status.yml</c> (currently: SC_BLADESTOP,
    /// SC_CURSEDCIRCLE_TARGET, SC__MANHOLE). Re-audit when status.yml
    /// adds new MOBLOSETARGET rows.
    /// </summary>
    private bool HasMobLoseTargetSc(MobEntity mob)
    {
        if (_sc == null) return false;
        // OPT1 CC bucket — Stone/Freeze/Stun/Sleep make the mob blank-target.
        if (_sc.Get(mob, StatusType.Stone) != null) return true;
        if (_sc.Get(mob, StatusType.Freeze) != null) return true;
        if (_sc.Get(mob, StatusType.Stun) != null) return true;
        if (_sc.Get(mob, StatusType.Sleep) != null) return true;
        // status.yml SCF_MOBLOSETARGET rows.
        if (_sc.Get(mob, StatusType.Bladestop) != null) return true;
        if (_sc.Get(mob, StatusType.CursedcircleTarget) != null) return true;
        if (_sc.Get(mob, StatusType.Manhole) != null) return true;
        return false;
    }

    /// <summary>
    /// rAthena <c>mob_add_spotted</c> (mob.cpp:112). Records every PC
    /// currently in view onto the mob's spotted log so the lazy AI
    /// keeps animating even after the PCs walk out of range. Called
    /// only on hard-AI ticks (lazy ticks prune via
    /// <see cref="MobSpotted.Clean"/>).
    /// </summary>
    /// <summary>
    /// AI-BOSS-ACTIVE-HP — the lazy-vs-hard gate (rAthena <c>mob_ai_sub_lazy</c>). A mob runs the
    /// hard AI when it has a master (slave coupling) or a PC is currently in view; otherwise it goes
    /// lazy <b>unless</b> a PC was in view within <c>mob_active_time</c> (<c>boss_active_time</c> for
    /// <c>MD_STATUSIMMUNE</c> bosses) ms — keeping it active briefly after the player steps out of view.
    /// </summary>
    internal bool ShouldRunLazy(MobEntity mob, bool pcInView, long nowTick)
    {
        if (mob.MasterId != null || pcInView) return false;
        var activeTime = (mob.Stats.Mode & MobMode.StatusImmune) != 0 ? BossActiveTimeMs : MobActiveTimeMs;
        var stayActive = activeTime > 0
            && _lastPcNear.TryGetValue(mob.Id, out var nearAt)
            && nowTick - nearAt < activeTime;
        return !stayActive;
    }

    /// <summary>
    /// AI-CHANGECHASE-VIS — rAthena <c>status_check_skilluse(src, target, 0, 0)</c>: can the mob
    /// currently perceive + reach the target? Combines the hide/cloak gate (<see cref="Status.EntityActionGates.CanSee"/>,
    /// the SC-based hiding set) with the line-of-sight path check the aggressive scan uses. Used to gate
    /// the changechase retarget so a mob doesn't switch onto a hidden / wall-blocked enemy.
    /// </summary>
    private bool Perceives(MobEntity mob, Entity target)
    {
        if (_sc != null && !mob.CanSee(target, _sc)) return false;
        if (_paths != null && !_paths.PathSearchLong(mob.MapId, mob.X, mob.Y, target.X, target.Y)) return false;
        return true;
    }

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
        // MOBAI-03 — rAthena mob.cpp:1851 clears attacked_id after the change-target decision so the
        // next think doesn't re-process the same attacker. TrySetTarget has already gated the switch
        // on CanChangeTarget + folded the attacker into TargetId; the rude skill/escape below targets
        // via TargetId (or the `attacker` arg), so dropping the fallback id here is safe.
        mob.AttackedId = 0;

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
