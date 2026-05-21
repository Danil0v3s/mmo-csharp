using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_FIRE_CLOAK — Elemental Fire Cloak. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/firecloak.cpp</c>.
/// Toggles SC_FIRE_CLOAK on target + SC_FIRE_CLOAK_OPTION on the
/// elemental.
/// </summary>
public sealed class FireCloak : SkillImpl
{
    public FireCloak() : base(SkillIds.EL_FIRE_CLOAK) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.FireCloak) != null
            || ctx.Sc?.Get(src, StatusType.FireCloakOption) != null)
        {
            ctx.Sc.End(target, StatusType.FireCloak);
            ctx.Sc.End(src, StatusType.FireCloakOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.FireCloakOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.FireCloak, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
