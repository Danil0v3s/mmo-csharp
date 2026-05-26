using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_SHIELDPRESS — Royal Guard Shield Press. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/shieldpress.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 200*lv) + base STR</c>; with
/// <see cref="StatusType.ShieldPower"/> active, plus
/// <c>15*lv * IG_SHIELD_MASTERY_lv</c>.</para>
///
/// <para>🚩 INFRA-DEFERRED — rAthena also adds shield weight /10 from
/// the equipped left-hand armor row; requires
/// <c>IEquipService.FindEquipped</c> threading into the calc path.</para>
/// </summary>
public sealed class ShieldPress : WeaponSkillImpl
{
    public ShieldPress() : base(SkillIds.LG_SHIELDPRESS) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 200 * skillLevel) + src.Stats.Str;
        if (ctx.Sc?.Get(src, StatusType.ShieldPower) != null && src is PlayerEntity sd)
            ratio += skillLevel * 15 * (ctx.PlayerSkill?.CheckSkill(sd, SkillIds.IG_SHIELD_MASTERY) ?? 0);
        return ratio;
    }
}
