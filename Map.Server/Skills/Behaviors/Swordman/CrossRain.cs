using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_CROSS_RAIN — Imperial Guard Cross Rain. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/crossrain.cpp</c>.
///
/// <para>Drops a damage unit at (x, y). Per-level ratio:
/// <c>(450 + 10*IG_SPEAR_SWORD_M_lv)</c> normally; with
/// <see cref="StatusType.HolyS"/> the per-level base jumps to
/// <c>(650 + 15*IG_SPEAR_SWORD_M_lv)</c>. Plus a flat <c>7*SPL</c>.</para>
/// </summary>
public sealed class CrossRain : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public CrossRain() : base(SkillIds.IG_CROSS_RAIN) { }

    public CrossRain(ISkillUnitService? units = null) : base(SkillIds.IG_CROSS_RAIN)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var mastery = (src is PlayerEntity sd)
            ? (ctx.PlayerSkill?.CheckSkill(sd, SkillIds.IG_SPEAR_SWORD_M) ?? 0)
            : 0;
        var holy = ctx.Sc?.Get(src, StatusType.HolyS) != null;
        var perLevel = holy ? (650 + 15 * mastery) : (450 + 10 * mastery);
        return baseRatio + (-100 + perLevel * skillLevel) + 7 * src.Stats.Spl;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
