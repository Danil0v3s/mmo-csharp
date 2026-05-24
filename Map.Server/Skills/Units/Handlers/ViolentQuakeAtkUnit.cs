using Map.Server.Entities;

namespace Map.Server.Skills.Units.Handlers;

/// <summary>
/// AG_VIOLENT_QUAKE_ATK — Arch Mage Violent Quake rising-rock sub-unit.
/// Each rock fires once at its assigned stagger time, dealing earth
/// magic damage to its single covered cell. rAthena
/// <c>violentquake.cpp:60</c>:
/// <c>skillratio += -100 + 200 + 1200 * skill_lv + 5 * sstatus->spl</c>.
/// </summary>
public sealed class ViolentQuakeAtkUnit : ISkillUnitTickHandler
{
    public ushort SkillId => SkillIds.AG_VIOLENT_QUAKE_ATK;

    // Sub-units are one-shot: very short duration with a single tick
    // at the stagger boundary. SkillUnitService's deferred-start gate
    // (group.StartAt) carries the actual fire time; here we just need
    // enough lifetime for the first tick to land.
    public int DurationMs(ushort skillLevel) => 300;
    public int IntervalMs(ushort skillLevel) => 300;

    /// <summary>3-cell radius (7×7) — rAthena skill_db
    /// AG_VIOLENT_QUAKE_ATK.Unit.Range = 3 covers the rising-rock
    /// splash. The parent AG_VIOLENT_QUAKE handles the spawn-area
    /// randomization.</summary>
    public int Radius(ushort skillLevel) => 0; // single-cell — the parent randomizes the spawn cell

    public void OnTick(Entity? caster, Entity victim, ushort skillLevel, long tick, ISkillUnitContext ctx)
    {
        if (caster == null) return;
        // rAthena: ratio = -100 + 200 + 1200 * lv + 5 * spl.
        var ratio = -100 + 200 + 1200 * skillLevel + 5 * caster.Stats.Spl;
        // Magic base — same approximation Storm Gust uses until the
        // renewal MATK formula lands in BattleCalculator.
        var matkAvg = (caster.Stats.MatkMin + caster.Stats.MatkMax) / 2;
        var dmg = (int)Math.Clamp((long)matkAvg * ratio / 100, 0, int.MaxValue);
        if (dmg > 0) ctx.Damage.ApplyDamage(victim, dmg, caster);
    }
}
