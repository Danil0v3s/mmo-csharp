using Map.Server.Entities;

namespace Map.Server.Skills.Units.Handlers;

/// <summary>
/// WZ_STORMGUST — wind magic AoE on an 11×11 zone for 4.5s, ticking
/// every 450 ms. Each tick deals <c>MatkMin * (skill_lv + 4) / 4</c>
/// to every enemy on a covered cell and (in rAthena) accumulates
/// freeze-strike counts that culminate in <c>SC_FREEZE</c>; the
/// freeze accumulator lives in the skill resolver, not here.
/// </summary>
public sealed class StormGustUnit : ISkillUnitTickHandler
{
    public ushort SkillId => SkillIds.WZ_STORMGUST;

    public int DurationMs(ushort skillLevel) => 4_500;
    public int IntervalMs(ushort skillLevel) => 450;
    public int Radius(ushort skillLevel) => 5;  // 11x11

    public void OnTick(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx)
    {
        if (caster == null) return;
        var dmg = (int)(caster.Stats.MatkMin * (skillLevel + 4) / 4);
        if (dmg > 0) ctx.Damage.ApplyDamage(victim, dmg, caster);
    }
}
