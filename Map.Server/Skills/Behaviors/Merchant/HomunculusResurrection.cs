using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_RESURRECTHOMUN — Alchemist Homunculus Resurrection
/// (skill.cpp:AM_RESURRECTHOMUN arm). Revives the caster's dead
/// homunculus at <c>20*lv %</c> HP via
/// <see cref="IHomunculusService.Resurrect"/>. Refuses with
/// SkillFail when no homunculus record exists.
/// </summary>
public sealed class HomunculusResurrection : SkillImpl
{
    public HomunculusResurrection() : base(SkillIds.AM_RESURRECTHOMUN) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        var percent = (byte)Math.Min(100, 20 * skillLevel);
        var ok = ctx.Homunculus?.Resurrect(pc, percent, x, y) ?? 0;
        if (ok == 0)
        {
            ctx.Client?.BroadcastSkillFail(pc, SkillId,
                Core.Server.Packets.Out.ZC.SkillFailCause.SummonNone);
        }
    }
}
