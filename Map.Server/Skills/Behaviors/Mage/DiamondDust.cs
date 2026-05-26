using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_DIAMONDDUST — Sorcerer Diamond Dust. Manual port of
/// <c>rathena-fork/src/map/skills/mage/diamonddust.cpp</c>.
///
/// <para>Ground unit Water magic. Ratio:
/// <c>+(-100 + 2*INT) + 300*FrostWeaponLv + INT*lv</c>. SA_FROSTWEAPON
/// is read via <c>ctx.PlayerSkill.CheckSkill</c>; SC_COOLER_OPTION on
/// the caster adds <c>job_level*5</c> to the ratio. Splash victims roll
/// <c>(5 + 5*lv) %</c> SC_CRYSTALIZE (+job_level/5 with SC_COOLER_OPTION).</para>
/// </summary>
public sealed class DiamondDust : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly Random _rng;

    public DiamondDust() : base(SkillIds.SO_DIAMONDDUST) => _rng = Random.Shared;

    public DiamondDust(ISkillUnitService? units = null, Random? rng = null) : base(SkillIds.SO_DIAMONDDUST)
    {
        _units = units;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skillratio += -100 + 2*INT + 300*pc_checkskill(SA_FROSTWEAPON) + INT*lv.
        // +job_level*5 when SC_COOLER_OPTION is active on the caster.
        var pc = src as PlayerEntity;
        var frost = (pc != null) ? (ctx.PlayerSkill?.CheckSkill(pc, SkillIds.SA_FROSTWEAPON) ?? 0) : 0;
        var ratio = baseRatio + (-100 + 2 * src.Stats.IntStat) + 300 * frost + src.Stats.IntStat * skillLevel;
        if (ctx.Sc?.Get(src, StatusType.CoolerOption) != null)
            ratio += (pc?.JobLevel ?? 0) * 5;
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: rate = 5 + 5*lv (+ job_level/5 if SC_COOLER_OPTION).
        var rate = 5 + 5 * skillLevel;
        if (ctx.Sc?.Get(src, StatusType.CoolerOption) != null && src is PlayerEntity pc)
            rate += pc.JobLevel / 5;
        if (_rng.Next(100) < rate)
            ctx.Sc?.Start(target, StatusType.Crystalize, val1: skillLevel, 0, 0, 0, durationMs: 5000, src);
    }
}
