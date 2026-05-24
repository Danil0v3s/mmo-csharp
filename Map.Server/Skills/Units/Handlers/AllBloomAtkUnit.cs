using Map.Server.Entities;

namespace Map.Server.Skills.Units.Handlers;

/// <summary>
/// AG_ALL_BLOOM_ATK — Arch Mage All Bloom rose-bud sub-unit. Each bud
/// fires once at its stagger time, dealing fire magic damage. rAthena
/// <c>allbloom.cpp</c>'s ratio formula on the ATK arm scales with
/// caster SPL and skill level; the SC_CLIMAX modulation lives on the
/// parent AG_ALL_BLOOM plugin.
/// </summary>
public sealed class AllBloomAtkUnit : ISkillUnitTickHandler
{
    public ushort SkillId => SkillIds.AG_ALL_BLOOM_ATK;

    public int DurationMs(ushort skillLevel) => 300;
    public int IntervalMs(ushort skillLevel) => 300;

    public int Radius(ushort skillLevel) => 0; // parent randomizes the spawn cell

    public void OnTick(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx)
    {
        if (caster == null) return;
        // rAthena ratio approximation — same shape as the violent-quake
        // sub-unit (the rAthena yml ATK rows use ratio = base + per-lv +
        // 5*spl + RE_LVL_DMOD). For All Bloom the per-lv coefficient is
        // 1500, slightly stronger than ViolentQuake.
        var ratio = -100 + 250 + 1500 * skillLevel + 5 * caster.Stats.Spl;
        var matkAvg = (caster.Stats.MatkMin + caster.Stats.MatkMax) / 2;
        var dmg = (int)Math.Clamp((long)matkAvg * ratio / 100, 0, int.MaxValue);
        if (dmg > 0) ctx.Damage.ApplyDamage(victim, dmg, caster);
    }
}
