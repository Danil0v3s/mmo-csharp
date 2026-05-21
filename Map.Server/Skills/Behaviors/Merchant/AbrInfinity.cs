using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_SUMMON_ABR_INFINITY — Meister Summon ABR Infinity. Manual port
/// of <c>rathena-fork/src/map/skills/merchant/abrinfinity.cpp</c>.
/// </summary>
public sealed class AbrInfinity : SkillImpl
{
    public AbrInfinity() : base(SkillIds.MT_SUMMON_ABR_INFINITY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.AbrInfinity, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
    }
}
