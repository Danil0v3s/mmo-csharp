using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_SUMMON_ABR_DUAL_CANNON — Meister Summon ABR Dual Cannon. Manual
/// port of <c>rathena-fork/src/map/skills/merchant/abrdualcannon.cpp</c>.
/// Spawns an ABR pet (TODO); applies SC_ABR_DUAL_CANNON.
/// </summary>
public sealed class AbrDualCannon : SkillImpl
{
    public AbrDualCannon() : base(SkillIds.MT_SUMMON_ABR_DUAL_CANNON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.AbrDualCannon, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
    }
}
