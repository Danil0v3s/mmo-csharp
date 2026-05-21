using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_STRONG_PROTECTION — Elemental Strong Protection. Manual port
/// of <c>rathena-fork/src/map/skills/elemental/strongprotection.cpp</c>.
/// Toggles SC_STRONG_PROTECTION on target + SC_STRONG_PROTECTION_OPTION
/// on the elemental.
/// </summary>
public sealed class StrongProtection : SkillImpl
{
    public StrongProtection() : base(SkillIds.EM_EL_STRONG_PROTECTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.StrongProtection) != null
            || ctx.Sc?.Get(src, StatusType.StrongProtectionOption) != null)
        {
            ctx.Sc.End(target, StatusType.StrongProtection);
            ctx.Sc.End(src, StatusType.StrongProtectionOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.StrongProtectionOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.StrongProtection, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
