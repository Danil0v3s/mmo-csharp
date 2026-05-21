using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_UNLIMITED_HUMMING_VOICE — Minstrel/Wanderer Unlimited Humming
/// Voice. Manual port of <c>rathena-fork/src/map/skills/archer/unlimitedhummingvoice.cpp</c>.
/// Party-wide buff. Splash via party_foreachsamemap TODO.
/// </summary>
public sealed class UnlimitedHummingVoice : SkillImpl
{
    public UnlimitedHummingVoice() : base(SkillIds.WM_UNLIMITED_HUMMING_VOICE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Unlimitedhummingvoice, val1: skillLevel, val2: 5, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
