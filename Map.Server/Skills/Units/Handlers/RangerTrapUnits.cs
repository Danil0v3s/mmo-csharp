using Map.Server.Entities;

namespace Map.Server.Skills.Units.Handlers;

/// <summary>
/// COMBAT-55 — Ranger trap damage units. Each places a single-cell trap that
/// detonates on an enemy stepping onto it (<see cref="OnPlace"/>, one trigger per
/// entity entry), dealing <see cref="TrapDamage.Compute"/> (base <c>skill_lv*DEX +
/// INT*5</c> + <c>RE_LVL_TMDMOD</c> + the Research-Trap multiplier). The damage
/// ignores element/flee/def (NK_IGNORE*), so it is applied raw. The trap persists
/// for its <c>Duration1</c> (15 s) so it can catch successive enemies.
/// </summary>
public abstract class RangerTrapUnit : ISkillUnitTickHandler
{
    public abstract ushort SkillId { get; }
    public int DurationMs(ushort skillLevel) => 15_000; // skill_db Duration1
    public int IntervalMs(ushort skillLevel) => 1_000;
    public int Radius(ushort skillLevel) => 0; // single trap cell

    // The trap's recurring tick is inert; detonation fires on cell entry (OnPlace).
    public void OnTick(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx) { }

    public void OnPlace(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx)
    {
        if (caster == null) return;
        var dmg = TrapDamage.Compute(SkillId, skillLevel, caster);
        if (dmg <= 0) return;
        var clamped = dmg > int.MaxValue ? int.MaxValue : (int)dmg;
        ctx.Damage.ApplyDamage(victim, clamped, caster);
    }
}

/// <summary>RA_CLUSTERBOMB — divisor 50 (double the trap multiplier of the others).</summary>
public sealed class ClusterBombUnit : RangerTrapUnit
{
    public override ushort SkillId => SkillIds.RA_CLUSTERBOMB;
}

/// <summary>RA_FIRINGTRAP — divisor 100; the on-hit SC_BURNING lives on the skill plugin.</summary>
public sealed class FiringTrapUnit : RangerTrapUnit
{
    public override ushort SkillId => SkillIds.RA_FIRINGTRAP;
}

/// <summary>RA_ICEBOUNDTRAP — divisor 100; the on-hit SC_FREEZING lives on the skill plugin.</summary>
public sealed class IceboundTrapUnit : RangerTrapUnit
{
    public override ushort SkillId => SkillIds.RA_ICEBOUNDTRAP;
}
