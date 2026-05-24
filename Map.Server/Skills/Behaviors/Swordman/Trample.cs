using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_TRAMPLE — Royal Guard Trample. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/trample.cpp</c>.
/// 25 + 25*lv% chance to wipe traps in splash range, and ends
/// SC_SV_ROOTTWIST on the target. Splash trap dispel is TODO.
/// </summary>
public sealed class Trample : SkillImpl
{
    public Trample() : base(SkillIds.LG_TRAMPLE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Deferred: 25 + 25*lv % chance to dispel traps in splash range needs
        // the skill-unit (trap) enumeration path, which is not yet ported.
        ctx.Sc?.End(target, StatusType.SvRoottwist);
    }
}
