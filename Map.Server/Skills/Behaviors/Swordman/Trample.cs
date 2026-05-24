using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_TRAMPLE — Royal Guard Trample (skill.cpp:LG_TRAMPLE arm).
/// Rolls (25 + 25*lv) % per trap unit in a 5×5 splash to dispel it;
/// also ends <c>SC_SV_ROOTTWIST</c> on the target. The roll is per
/// trap group (matching rAthena: groups are dispelled atomically).
/// </summary>
public sealed class Trample : SkillImpl
{
    /// <summary>5×5 splash (rAthena Trample AOE).</summary>
    private const short SplashRadius = 2;

    private readonly Random _rng;

    public Trample() : base(SkillIds.LG_TRAMPLE) => _rng = Random.Shared;

    public Trample(Random? rng = null) : base(SkillIds.LG_TRAMPLE) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.End(target, StatusType.SvRoottwist);
        if (ctx.Units == null) return;
        var chance = 25 + 25 * skillLevel;
        var units = ctx.Units.GetUnitsInArea(target.MapId, target.X, target.Y, SplashRadius);
        var groups = new HashSet<SkillUnitGroup>();
        foreach (var u in units)
        {
            if (!IsTrap(u.Group.SkillId)) continue;
            groups.Add(u.Group);
        }
        foreach (var g in groups)
        {
            if (_rng.Next(100) < chance) ctx.Units.DelUnitGroup(g);
        }
    }

    private static bool IsTrap(ushort skillId) => skillId is
        SkillIds.HT_SKIDTRAP or
        SkillIds.HT_LANDMINE or
        SkillIds.HT_ANKLESNARE or
        SkillIds.HT_SHOCKWAVE or
        SkillIds.HT_SANDMAN or
        SkillIds.HT_FLASHER or
        SkillIds.HT_FREEZINGTRAP or
        SkillIds.HT_BLASTMINE or
        SkillIds.HT_CLAYMORETRAP or
        SkillIds.HT_TALKIEBOX or
        SkillIds.RA_CLUSTERBOMB or
        SkillIds.RA_FIRINGTRAP or
        SkillIds.RA_ICEBOUNDTRAP or
        SkillIds.RA_ELECTRICSHOCKER or
        SkillIds.RA_MAGENTATRAP or
        SkillIds.RA_COBALTTRAP or
        SkillIds.RA_MAIZETRAP or
        SkillIds.RA_VERDURETRAP;
}
