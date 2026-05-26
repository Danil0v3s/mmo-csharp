using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_EARTHDRIVE — Royal Guard Earth Drive. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/earthdrive.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 380*lv) + STR + VIT</c>; with
/// <see cref="StatusType.ShieldPower"/> active, plus
/// <c>lv * 37 * IG_SHIELD_MASTERY_lv</c>.</para>
///
/// <para>🚩 INFRA-DEFERRED — rAthena also runs
/// <c>map_foreachinarea(skill_cell_overlap)</c> to wipe ground-unit
/// cells (Pneuma, SafetyWall, etc.) inside the splash. The
/// <c>ISkillUnitService</c> cell-wipe API isn't ported yet.</para>
/// </summary>
public sealed class EarthDrive : RecursiveDamageSplashSkillImpl
{
    public EarthDrive() : base(SkillIds.LG_EARTHDRIVE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 380 * skillLevel) + src.Stats.Str + src.Stats.Vit;
        if (ctx.Sc?.Get(src, StatusType.ShieldPower) != null && src is PlayerEntity sd)
            ratio += skillLevel * 37 * (ctx.PlayerSkill?.CheckSkill(sd, SkillIds.IG_SHIELD_MASTERY) ?? 0);
        return ratio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
}
