using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_UNLIMITED_HUMMING_VOICE — Minstrel/Wanderer Unlimited Humming
/// Voice. Manual port of <c>rathena-fork/src/map/skills/archer/unlimitedhummingvoice.cpp</c>.
///
/// <para>Party-wide buff. val2 carries WM_LESSON. Caster gets the SC
/// then every nearby party member on the same map gets the same SC.</para>
/// </summary>
public sealed class UnlimitedHummingVoice : SkillImpl
{
    public UnlimitedHummingVoice() : base(SkillIds.WM_UNLIMITED_HUMMING_VOICE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var lesson = (src is PlayerEntity pc) ? (ctx.PlayerSkill?.CheckSkill(pc, SkillIds.WM_LESSON) ?? 0) : 0;
        ctx.Sc?.Start(src, StatusType.Unlimitedhummingvoice, val1: skillLevel, val2: lesson, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == pcSrc.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.Unlimitedhummingvoice, val1: skillLevel, val2: lesson, 0, 0, durationMs: 60_000, src);
            }, includeSelf: false);
        }
    }
}
