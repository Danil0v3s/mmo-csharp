using Map.Server.Entities;

namespace Map.Server.Skills.Units.Handlers;

/// <summary>
/// PR_MAGNUSEXORCISMUS — holy AoE magic damage on a 5×5 zone for 10s,
/// ticking every 1s. Each tick hits every enemy standing on a covered
/// cell with <c>MatkMin * (1 + skill_lv) / 5</c>. Matches the rAthena
/// formula in <c>src/map/skill.cpp</c> Magnus row, minus the holy-undead
/// 2× modifier (that lives in the damage calculator, not the unit
/// handler).
/// </summary>
public sealed class MagnusExorcismusUnit : ISkillUnitTickHandler
{
    public ushort SkillId => SkillIds.PR_MAGNUSEXORCISMUS;

    public int DurationMs(ushort skillLevel) => 10_000;
    public int IntervalMs(ushort skillLevel) => 1_000;
    public int Radius(ushort skillLevel) => 2;  // 5x5

    public void OnTick(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx)
    {
        if (caster == null) return;
        var dmg = (int)(caster.Stats.MatkMin * (1 + skillLevel) / 5);
        if (dmg > 0) ctx.Damage.ApplyDamage(victim, dmg, caster);
    }
}
