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
        if (target is PlayerEntity && ctx.Sc != null)
        {
            ctx.Sc.End(target, StatusType.Hiding);
            ctx.Sc.End(target, StatusType.Cloaking);
            ctx.Sc.End(target, StatusType.Cloakingexceed);
            ctx.Sc.End(target, StatusType.Camouflage);
            ctx.Sc.End(target, StatusType.Newmoon);
            ctx.Sc.End(target, StatusType.Shadowform);
            ctx.Sc.End(target, StatusType.Marionette);
            ctx.Sc.End(target, StatusType.Harmonize);
            // rAthena: on a successful trample, apply SC_KG_KAGEHUMI to the
            // victim (Duration1 ranges 5000..9000 ms by level per skill_db).
            var duration = 5_000 + (skillLevel - 1) * 1_000;
            ctx.Sc.Start(target, StatusType.Kagehumi, val1: skillLevel, 0, 0, 0, durationMs: duration, src);
        }
    }
}
