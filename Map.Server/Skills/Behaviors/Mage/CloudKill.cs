using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_CLOUD_KILL — Sorcerer Cloud Kill. Manual port of
/// <c>rathena-fork/src/map/skills/mage/cloudkill.cpp</c>.
///
/// <para>Drops a poisonous ground unit. Per-tick splash applies the
/// status configured on the skill (poison) with 100 % rate. Ratio:
/// <c>+(-100 + 40*lv) + INT*3</c>; SC_CURSED_SOIL_OPTION on caster
/// adds <c>job_level</c>, and SC_DEEP_POISONING_OPTION adds
/// <c>ratio * 1500 / 100</c> (15× bump).</para>
/// </summary>
public sealed class CloudKill : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public CloudKill() : base(SkillIds.SO_CLOUD_KILL) { }

    public CloudKill(ISkillUnitService? units = null) : base(SkillIds.SO_CLOUD_KILL)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skillratio += -100 + 40*lv + INT*3.
        var ratio = baseRatio + (-100 + 40 * skillLevel) + src.Stats.IntStat * 3;
        if (ctx.Sc != null)
        {
            if (ctx.Sc.Get(src, StatusType.CursedSoilOption) != null && src is PlayerEntity pc)
                ratio += pc.JobLevel;
            if (ctx.Sc.Get(src, StatusType.DeepPoisoningOption) != null)
                ratio += ratio * 1500 / 100;
        }
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: sc_start at 100% with skill's default SC (Poison) and time2 duration.
        ctx.Sc?.Start(target, StatusType.Poison, val1: skillLevel, 0, 0, 0, durationMs: 5000, src);
    }
}
