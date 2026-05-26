using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// PA_SHIELDCHAIN — Paladin Shield Chain. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/shieldchain.cpp</c>.
///
/// <para>Ratio: <c>-100 + 300 + 200*lv</c> base; when
/// <see cref="StatusType.ShieldPower"/> is active the running ratio
/// gets <c>14*lv * IG_SHIELD_MASTERY_lv</c> and then is multiplied
/// by 1.5 (rAthena always-on +50 %).</para>
///
/// <para>🚩 INFRA-DEFERRED — rAthena also adds shield weight /10 and
/// <c>4 * shield_refine</c>; requires
/// <c>IEquipService.FindEquipped(HandL)</c> threading.</para>
/// </summary>
public sealed class ShieldChain : WeaponSkillImpl
{
    public ShieldChain() : base(SkillIds.PA_SHIELDCHAIN) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        // rAthena renewal resets the running ratio for this skill.
        var ratio = -100 + 300 + 200 * skillLevel;
        if (ctx.Sc?.Get(src, StatusType.ShieldPower) != null)
        {
            if (src is PlayerEntity sd)
                ratio += skillLevel * 14 * (ctx.PlayerSkill?.CheckSkill(sd, SkillIds.IG_SHIELD_MASTERY) ?? 0);
            ratio += ratio * 50 / 100;
        }
        return ratio;
    }
}
