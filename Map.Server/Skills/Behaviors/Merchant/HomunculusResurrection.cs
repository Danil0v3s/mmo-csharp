using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_RESURRECTHOMUN — Alchemist Homunculus Resurrection. Manual port
/// of <c>rathena-fork/src/map/skills/merchant/homunculusresurrection.cpp</c>.
/// Revives caster's dead homunculus at <c>20*lv %</c> HP. hom_resurrect
/// service TODO.
/// </summary>
public sealed class HomunculusResurrection : SkillImpl
{
    public HomunculusResurrection() : base(SkillIds.AM_RESURRECTHOMUN) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: IHomunculusService.Resurrect(sd, 20*lv, x, y) — currently a no-op.
    }
}
