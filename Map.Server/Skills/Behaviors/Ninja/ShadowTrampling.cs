using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KG_KAGEHUMI — Shadow Trampling. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/shadowtrampling.cpp</c>.
/// Splash dispel that ends Hiding / Cloaking / Camouflage / Shadow
/// Form / Marionette / Harmonize on enemies; on success applies
/// SC_KG_KAGEHUMI. Splash iteration is TODO.
/// </summary>
public sealed class ShadowTrampling : SkillImpl
{
    public ShadowTrampling() : base(SkillIds.KG_KAGEHUMI) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (target is PlayerEntity)
        {
            ctx.Sc?.End(target, StatusType.Hiding);
            // TODO: end SC_CLOAKING / SC_CLOAKINGEXCEED / SC_CAMOUFLAGE / SC_NEWMOON / SC__SHADOWFORM / SC_MARIONETTE / SC_HARMONIZE.
        }
    }
}
