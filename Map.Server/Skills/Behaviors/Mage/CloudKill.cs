using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_CLOUD_KILL — Sorcerer Cloud Kill. Manual port of
/// <c>rathena-fork/src/map/skills/mage/cloudkill.cpp</c>.
///
/// <para>Drops a poisonous ground unit. Per-tick splash applies the
/// status configured on the skill (poison) with 100 % rate. Ratio:
/// <c>+(-100 + 40*lv) + INT*3</c>, with optional SC_CURSED_SOIL_OPTION
/// (+job_level) and SC_DEEP_POISONING_OPTION (×16) bonuses — SC reads
/// on caster TODO.</para>
/// </summary>
public sealed class CloudKill : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public CloudKill() : base(SkillIds.SO_CLOUD_KILL) { }

    public CloudKill(ISkillUnitService? units = null) : base(SkillIds.SO_CLOUD_KILL)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + 40*lv + INT*3; SC bonuses on caster TODO (no buff readback here).
        return baseRatio + (-100 + 40 * skillLevel) + src.Stats.IntStat * 3;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: sc_start at 100% with skill's default SC (Poison) and time2 duration.
        ctx.Sc?.Start(target, StatusType.Poison, val1: skillLevel, 0, 0, 0, durationMs: 5000, src);
    }
}
