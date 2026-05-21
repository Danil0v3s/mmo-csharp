using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BD_ADAPTATION — Bard Encore (Amp / Adaptation). Manual port of
/// <c>rathena-fork/src/map/skills/archer/amp.cpp</c>.
///
/// <para>Renewal: lets the caster instantly re-cast their last song
/// (handled by <see cref="StatusSkillImpl"/> default). Pre-renewal:
/// ends SC_DANCING on the target. We hand off to the base behavior
/// (renewal path) and additionally cancel SC_DANCING for parity with
/// the legacy branch.</para>
/// </summary>
public sealed class Amp : StatusSkillImpl
{
    public Amp() : base(SkillIds.BD_ADAPTATION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.End(target, StatusType.Dancing);
    }
}
