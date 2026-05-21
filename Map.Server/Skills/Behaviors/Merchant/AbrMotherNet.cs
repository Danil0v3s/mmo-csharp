using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_SUMMON_ABR_MOTHER_NET — Meister Summon ABR Mother Net.
/// Manual port of <c>rathena-fork/src/map/skills/merchant/abrmothernet.cpp</c>.
/// </summary>
public sealed class AbrMotherNet : SkillImpl
{
    public AbrMotherNet() : base(SkillIds.MT_SUMMON_ABR_MOTHER_NET) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.AbrMotherNet, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
    }
}
