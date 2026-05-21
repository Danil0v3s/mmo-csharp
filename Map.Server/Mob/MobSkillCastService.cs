using Map.Server.Entities;
using Map.Server.Mob.Conditions;
using Map.Server.Skills;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Mob;

/// <summary>
/// Port of rAthena <c>mobskill_use</c> (<c>mob.cpp:4275-4502</c>).
/// Walks the mob's skill list, applies state + condition + permillage
/// gates, resolves a target via <see cref="MobSkillTargetResolver"/>,
/// and dispatches the cast through <see cref="ISkillCastService"/>.
///
/// <para>Split from <see cref="MobAiService"/> so the picker has a
/// dedicated test surface; <see cref="MobAiService"/> now delegates
/// to this service for both passive (idle) and event-driven trigger
/// dispatch.</para>
/// </summary>
public sealed class MobSkillCastService : IMobSkillCastService
{
    private readonly ISkillCastService? _skillCast;
    private readonly MobSkillConditionRegistry _conditions;
    private readonly MobSkillTargetResolver _targets;
    private readonly Random _rng;
    private readonly ILogger<MobSkillCastService> _logger;

    /// <summary>Per-mob, per-skill cooldown anchor (Environment.TickCount64).</summary>
    private readonly Dictionary<(EntityId mobId, int skillIndex), long> _skillDelay = new();

    private readonly Slaves.ISlaveMobService? _slaves;

    public MobSkillCastService(
        MobSkillConditionRegistry conditions,
        MobSkillTargetResolver targets,
        ILogger<MobSkillCastService> logger,
        ISkillCastService? skillCast = null,
        Random? rng = null,
        Slaves.ISlaveMobService? slaves = null)
    {
        _conditions = conditions;
        _targets = targets;
        _logger = logger;
        _skillCast = skillCast;
        _rng = rng ?? Random.Shared;
        _slaves = slaves;
    }

    /// <inheritdoc/>
    public bool TryUseSkill(MobEntity mob, long nowTick, int @event = -1, int damage = 0)
    {
        // Outer guards — rAthena mob.cpp:4286.
        if (_skillCast == null) return false;
        if (mob.DbEntry == null || mob.DbEntry.Skills.Count == 0) return false;
        if ((mob.Stats.Mode & MobMode.NoCast) != 0) return false;

        return RunPicker(mob, nowTick, @event, damage, recentSrc: null, triggerSkillId: 0);
    }

    /// <inheritdoc/>
    public bool NotifyEvent(MobEntity mob, Entity? src, long nowTick, MobSkillCondition trigger, int damage = 0, ushort triggerSkillId = 0)
    {
        if (_skillCast == null) return false;
        if (mob.DbEntry == null || mob.DbEntry.Skills.Count == 0) return false;
        if ((mob.Stats.Mode & MobMode.NoCast) != 0) return false;

        return RunPicker(mob, nowTick, (int)trigger, damage, recentSrc: src, triggerSkillId: triggerSkillId);
    }

    /// <summary>
    /// Inner loop. Walks mob_skill_db rows in random-start order
    /// (rAthena <c>battle_config.mob_ai &amp; 0x100</c>), applies the
    /// 5-gate filter (state / cooldown / event-or-passive / condition
    /// / permillage), and dispatches the first row whose target also
    /// resolves to a valid entity.
    /// </summary>
    private bool RunPicker(MobEntity mob, long nowTick, int @event, int damage, Entity? recentSrc, ushort triggerSkillId)
    {
        var skills = mob.DbEntry!.Skills;
        var start = _rng.Next(skills.Count);

        for (var n = 0; n < skills.Count; n++)
        {
            var i = (start + n) % skills.Count;
            var entry = skills[i];

            // (1) state — rAthena mob.cpp:4307.
            // Row state must match the mob's current skill-state, with
            // two exceptions: ANY rows always pass, and ANYTARGET rows
            // pass any state if the mob has a target (and we're not in
            // LOOT). MSS_DEAD blocks everything.
            if (mob.SkillState == MobSkillState.Dead) continue;
            if (entry.State != mob.SkillState)
            {
                var isAny = entry.State == MobSkillState.Any;
                var isAnyTarget =
                    entry.State == MobSkillState.AnyTarget
                    && mob.TargetId != 0
                    && mob.SkillState != MobSkillState.Loot;
                if (!isAny && !isAnyTarget) continue;
            }

            // (2) cooldown — mob.cpp:4302.
            var key = (mob.Id, i);
            if (_skillDelay.TryGetValue(key, out var readyAt) && readyAt > nowTick) continue;

            // (3) permillage — mob.cpp:4315 (note: rAthena checks this
            // BEFORE the cond1==event short-circuit, so an event-driven
            // trigger is still subject to the rate roll).
            if (_rng.Next(10_000) >= entry.Permillage) continue;

            // (4) condition / event match — mob.cpp:4318.
            if (!ConditionPasses(entry, mob, nowTick, @event, damage, recentSrc, triggerSkillId))
                continue;

            // (5) target resolution — mob.cpp:4392-4475.
            // For ground modes (MST_AROUND*) the picker resolves a
            // (x,y) cell and dispatches via StartCastAt; for everything
            // else it picks a single target entity.
            SkillCastResult castResult;
            if (IsGroundTarget(entry.Target))
            {
                var cell = _targets.ResolveGroundCell(mob, entry.Target);
                if (cell == null) continue;
                castResult = _skillCast!.StartCastAt(mob, cell.Value.x, cell.Value.y, entry.SkillId, entry.SkillLevel);
            }
            else
            {
                var target = _targets.ResolveEntity(mob, entry.Target);
                if (target == null) continue;
                castResult = _skillCast!.StartCast(mob, target.Id, entry.SkillId, entry.SkillLevel);
            }
            if (castResult != SkillCastResult.Started) continue;

            _skillDelay[key] = nowTick + Math.Max(1, entry.DelayMs);
            // T4.7 — record the cast id for MSC_AFTERSKILL on the next
            // think-tick. rAthena keeps this on md->ud.skill_id.
            mob.LastCastSkillId = entry.SkillId;
            _logger.LogDebug(
                "mob {Mob} cast {Skill}@{Level} via {Mode} (event={Event}, row={Row})",
                mob.Id, entry.SkillId, entry.SkillLevel, entry.Target, @event, i);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Condition gate. rAthena <c>mob.cpp:4318</c>. Two branches:
    /// <list type="bullet">
    ///   <item>Event-driven (event != -1): row's cond1 must equal the
    ///         incoming event for a direct match, plus the three special
    ///         cases (MSC_SKILLUSED / MSC_GROUNDATTACKED / MSC_DAMAGEDGT).</item>
    ///   <item>Passive (event == -1): walk the full MSC_* switch via the
    ///         <see cref="MobSkillConditionRegistry"/> + a context bag.</item>
    /// </list>
    /// </summary>
    /// <summary>
    /// rAthena <c>skill_get_casttype(skill_id) == CAST_GROUND</c>
    /// (mob.cpp:4389) — we approximate with the MST_AROUND* target
    /// modes since those are the only mob_skill_db modes that require
    /// a (x,y) cell. MST_TARGET cells for ground skills still resolve
    /// to the target entity's cell, which the picker reads via
    /// MobSkillTargetResolver.ResolveGroundCell.
    /// </summary>
    private static bool IsGroundTarget(MobSkillTarget mode) => mode is
        MobSkillTarget.Around1 or MobSkillTarget.Around2
        or MobSkillTarget.Around3 or MobSkillTarget.Around4
        or MobSkillTarget.Around5 or MobSkillTarget.Around6
        or MobSkillTarget.Around7 or MobSkillTarget.Around8;

    private bool ConditionPasses(MobSkillEntry entry, MobEntity mob, long nowTick, int @event, int damage, Entity? recentSrc, ushort triggerSkillId)
    {
        // --- special cases first (override the direct match below) ---

        // MSC_SKILLUSED — event must be SKILLUSED + skill_id matches cond2.
        if (entry.Condition == MobSkillCondition.SkillUsed)
        {
            if (@event != (int)MobSkillCondition.SkillUsed) return false;
            if (entry.Cond2 == 0) return true;
            return triggerSkillId != 0 && (int)triggerSkillId == entry.Cond2;
        }

        // MSC_GROUNDATTACKED — fires when ground-skill payload + damage > 0.
        if (entry.Condition == MobSkillCondition.GroundAttacked)
        {
            if (damage <= 0) return false;
            return @event == (int)MobSkillCondition.GroundAttacked;
        }

        // MSC_DAMAGEDGT — gated by damage > cond2. Direct event match
        // alone is NOT enough; the damage value must clear cond2.
        if (entry.Condition == MobSkillCondition.DamagedGreater)
        {
            if (damage <= 0) return false;
            if (@event == (int)MobSkillCondition.SkillUsed) return false;
            return damage > entry.Cond2;
        }

        // --- direct event-id match (CLOSEDATTACKED / LONGRANGEATTACKED /
        //     RUDEATTACKED / etc.) ---
        if (@event != -1 && @event == (int)entry.Condition)
            return true;

        // --- passive idle tick — full MSC_* switch through the registry ---
        if (@event != -1) return false;
        var evaluator = _conditions.Get(entry.Condition);
        if (evaluator == null) return false;
        var ctx = new MobConditionContext
        {
            Tick = nowTick,
            Target = mob.TargetId != 0 ? null /* picked downstream */ : null,
            Attacker = recentSrc,
            CumulativeDamageTaken = damage,
            SkillUsedNearby = triggerSkillId,
            Slaves = _slaves,
        };
        return evaluator.IsMet(mob, entry, ctx);
    }
}
