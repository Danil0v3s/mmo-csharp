using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_REST — Vaporize (Homunculus Rest, skill.cpp:AM_REST arm).
/// Calls <see cref="IHomunculusService.Vaporize"/> with the
/// <c>HOM_ST_REST</c> flag — the homunculus enters the resting state
/// and is removed from the world until the next <c>AM_CALLHOMUN</c>
/// wakes it back.
/// </summary>
public sealed class Vaporize : SkillImpl
{
    /// <summary>rAthena HOM_ST_REST (homunculus.hpp) — vaporize-flag
    /// "owner asked it to rest" branch.</summary>
    private const byte HomStRest = 1;

    public Vaporize() : base(SkillIds.AM_REST) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var ok = ctx.Homunculus?.Vaporize(pc, HomStRest) ?? 0;
        if (ok == 0)
        {
            ctx.Client?.BroadcastSkillFail(pc, SkillId,
                Core.Server.Packets.Out.ZC.SkillFailCause.SummonNone);
        }
    }
}
