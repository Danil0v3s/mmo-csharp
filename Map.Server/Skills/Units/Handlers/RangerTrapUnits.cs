using System;
using Map.Server.Entities;
using Map.Server.Skills.Splash;
using Map.Server.Status;

namespace Map.Server.Skills.Units.Handlers;

/// <summary>
/// COMBAT-55/74 — Ranger trap damage units. Each places a single-cell trap that detonates the
/// instant an enemy steps onto it (<see cref="OnPlace"/>). On detonation it deals
/// <see cref="TrapDamage.Compute"/> (base <c>skill_lv*DEX + INT*5</c> + <c>RE_LVL_TMDMOD</c> +
/// the Research-Trap multiplier) over a <b>Range-3 splash</b> (rAthena <c>Splash:true Range:3</c>),
/// applies the trap's on-hit SC to each victim, and is then <b>consumed</b> (rAthena
/// <c>skill_delunit</c>). The damage ignores element/flee/def (NK_IGNORE*), so it is applied raw.
///
/// <para>COMBAT-74 — the splash + consume use the injected
/// <see cref="IMapForeachInRangeService"/> (BCT_Enemy allegiance) + a <see cref="ISkillUnitService"/>
/// (via a <see cref="Lazy{T}"/> seam to avoid the SkillUnitService → handler → SkillUnitService
/// construction cycle). When neither is wired (unit tests with the parameterless ctor) OnPlace
/// falls back to hitting the single stepper, which is the COMBAT-55 behavior.</para>
/// </summary>
public abstract class RangerTrapUnit : ISkillUnitTickHandler
{
    private readonly IMapForeachInRangeService? _splash;
    private readonly Lazy<ISkillUnitService>? _unitsLazy;

    protected RangerTrapUnit(IMapForeachInRangeService? splash = null, Lazy<ISkillUnitService>? unitsLazy = null)
    {
        _splash = splash;
        _unitsLazy = unitsLazy;
    }

    public abstract ushort SkillId { get; }
    public int DurationMs(ushort skillLevel) => 15_000; // skill_db Duration1
    public int IntervalMs(ushort skillLevel) => 1_000;
    public int Radius(ushort skillLevel) => 0; // single trap cell (the trigger); splash is Range 3

    // The trap's recurring tick is inert; detonation fires on cell entry (OnPlace).
    public void OnTick(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx) { }

    public void OnPlace(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx, SkillUnitGroup group)
    {
        if (caster == null) return;
        // Guard against a second stepper re-detonating the (already-consumed) trap in the same tick.
        if (group.Units.Count > 0 && group.Units[0].Removed) return;

        var dmg = TrapDamage.Compute(SkillId, skillLevel, caster);
        var clamped = dmg <= 0 ? 0 : (dmg > int.MaxValue ? int.MaxValue : (int)dmg);

        if (_splash != null)
        {
            // Range-3 splash over enemies (BCT_Enemy → excludes caster + allies).
            _splash.ForEachEnemyInSplash(caster, victim.X, victim.Y, 3, e =>
            {
                if (clamped > 0) ctx.Damage.ApplyDamage(e, clamped, caster);
                ApplyTrapSc(e, skillLevel, caster, ctx.Sc);
            });
        }
        else
        {
            // No splash service (unit tests) — single stepper, matching COMBAT-55.
            if (clamped > 0) ctx.Damage.ApplyDamage(victim, clamped, caster);
            ApplyTrapSc(victim, skillLevel, caster, ctx.Sc);
        }

        // Consume the trap on detonation (rAthena skill_delunit).
        _unitsLazy?.Value?.DelUnitGroup(group);
    }

    /// <summary>The trap's on-detonation status effect, applied per splash victim. Base = none.</summary>
    protected virtual void ApplyTrapSc(Entity victim, ushort skillLevel, Entity caster, IStatusChangeService? sc) { }
}

/// <summary>RA_CLUSTERBOMB — divisor 50 (double the trap multiplier of the others); no on-hit SC.</summary>
public sealed class ClusterBombUnit : RangerTrapUnit
{
    public ClusterBombUnit(IMapForeachInRangeService? splash = null, Lazy<ISkillUnitService>? unitsLazy = null)
        : base(splash, unitsLazy) { }
    public override ushort SkillId => SkillIds.RA_CLUSTERBOMB;
}

/// <summary>RA_FIRINGTRAP — divisor 100; on-hit SC_BURNING (mirrors FiringTrap.ApplyAdditionalEffects).</summary>
public sealed class FiringTrapUnit : RangerTrapUnit
{
    public FiringTrapUnit(IMapForeachInRangeService? splash = null, Lazy<ISkillUnitService>? unitsLazy = null)
        : base(splash, unitsLazy) { }
    public override ushort SkillId => SkillIds.RA_FIRINGTRAP;

    protected override void ApplyTrapSc(Entity victim, ushort skillLevel, Entity caster, IStatusChangeService? sc)
        => sc?.Start(victim, StatusType.Burning, val1: skillLevel, val2: 1000, val3: (int)caster.Id, 0,
            durationMs: 10_000, caster);
}

/// <summary>RA_ICEBOUNDTRAP — divisor 100; on-hit SC_FREEZING (mirrors IceboundTrap.ApplyAdditionalEffects).</summary>
public sealed class IceboundTrapUnit : RangerTrapUnit
{
    public IceboundTrapUnit(IMapForeachInRangeService? splash = null, Lazy<ISkillUnitService>? unitsLazy = null)
        : base(splash, unitsLazy) { }
    public override ushort SkillId => SkillIds.RA_ICEBOUNDTRAP;

    protected override void ApplyTrapSc(Entity victim, ushort skillLevel, Entity caster, IStatusChangeService? sc)
        => sc?.Start(victim, StatusType.Freezing, val1: skillLevel, 0, 0, 0, durationMs: 8000, caster);
}
