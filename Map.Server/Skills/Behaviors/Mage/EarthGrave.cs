using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_EARTHGRAVE — Sorcerer Earth Grave. Manual port of
/// <c>rathena-fork/src/map/skills/mage/earthgrave.cpp</c>.
///
/// <para>Ground unit Earth magic. Ratio:
/// <c>+(-100 + 2*INT) + 300*SeismicWeaponLv + INT*lv</c>. SA_SEISMICWEAPON
/// is read via <c>ctx.PlayerSkill.CheckSkill</c>; SC_CURSED_SOIL_OPTION
/// on the caster adds <c>job_level*5</c> to the ratio. Splash victims
/// roll <c>5*lv %</c> SC_BLEEDING.</para>
/// </summary>
public sealed class EarthGrave : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly Random _rng;

    public EarthGrave() : base(SkillIds.SO_EARTHGRAVE) => _rng = Random.Shared;

    public EarthGrave(ISkillUnitService? units = null, Random? rng = null) : base(SkillIds.SO_EARTHGRAVE)
    {
        _units = units;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skillratio += -100 + 2*INT + 300*pc_checkskill(SA_SEISMICWEAPON) + INT*lv.
        // +job_level*5 when SC_CURSED_SOIL_OPTION is active on the caster.
        var pc = src as PlayerEntity;
        var seismic = (pc != null) ? (ctx.PlayerSkill?.CheckSkill(pc, SkillIds.SA_SEISMICWEAPON) ?? 0) : 0;
        var ratio = baseRatio + (-100 + 2 * src.Stats.IntStat) + 300 * seismic + src.Stats.IntStat * skillLevel;
        if (ctx.Sc?.Get(src, StatusType.CursedSoilOption) != null)
            ratio += (pc?.JobLevel ?? 0) * 5;
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 5 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Bleeding, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 10_000, src);
    }
}
