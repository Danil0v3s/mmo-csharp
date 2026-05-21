using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BA_PANGVOICE — Bard Pang Voice. Manual port of
/// <c>rathena-fork/src/map/skills/archer/pangvoice.cpp</c>.
/// Renewal: SC_CONFUSION + SC_BLEEDING at 100 % base.
/// </summary>
public sealed class PangVoice : SkillImpl
{
    public PangVoice() : base(SkillIds.BA_PANGVOICE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Confusion, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Sc?.Start(target, StatusType.Bleeding, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
