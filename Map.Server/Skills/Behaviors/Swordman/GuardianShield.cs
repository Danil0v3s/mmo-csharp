using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_GUARDIAN_SHIELD — Imperial Guard Guardian Shield. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/guardianshield.cpp</c>.
/// Solo or no-party cast applies SC_GUARDIAN_SHIELD to the target. With
/// a party, splashes the same SC to nearby party members. SC enum +
/// party splash are TODO.
/// </summary>
public sealed class GuardianShield : SkillImpl
{
    public GuardianShield() : base(SkillIds.IG_GUARDIAN_SHIELD) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Deferred: SC_GUARDIAN_SHIELD is not yet defined in StatusType. Once
        // the enum lands, the solo branch starts it on the target and the
        // party branch splashes it via ctx.PartyMap?.ForEachOnSameMap.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
