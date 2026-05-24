using Map.Server.Entities;
using Map.Server.Skills;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MA_REMOVETRAP — Mercenary Remove Trap (skill.cpp:MA_REMOVETRAP
/// arm). Same shape as HT_REMOVETRAP — walks BL_SKILL groups on the
/// targeted cell and expires any whose skill id is in the trap
/// roster.
/// </summary>
public sealed class MercenaryRemoveTrap : SkillImpl
{
    public MercenaryRemoveTrap() : base(SkillIds.MA_REMOVETRAP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (ctx.Units == null) return;
        foreach (var u in ctx.Units.GetUnitsInArea(target.MapId, target.X, target.Y, radius: 0))
        {
            if (!IsTrap(u.Group.SkillId)) continue;
            ctx.Units.DelUnitGroup(u.Group);
            break;
        }
    }

    private static bool IsTrap(ushort skillId) => skillId is
        SkillIds.HT_SKIDTRAP or SkillIds.HT_LANDMINE or SkillIds.HT_ANKLESNARE
        or SkillIds.HT_SHOCKWAVE or SkillIds.HT_SANDMAN or SkillIds.HT_FLASHER
        or SkillIds.HT_FREEZINGTRAP or SkillIds.HT_BLASTMINE or SkillIds.HT_CLAYMORETRAP
        or SkillIds.HT_TALKIEBOX;
}
