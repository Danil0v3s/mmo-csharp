using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_STEALCOIN — Mug. Manual port of
/// <c>rathena-fork/src/map/skills/thief/mug.cpp</c>.
/// Rate = <c>10*RG_STEALCOIN + dex/2 + luk/2 + 2*(baseLv - targetLv)</c>
/// per 1000. On success deals 0 damage + steals zeny. Steal-coin
/// implementation deferred — animation only.
/// </summary>
public sealed class Mug : SkillImpl
{
    public Mug() : base(SkillIds.RG_STEALCOIN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Deferred: mob steal-coin flag + zeny credit — needs mob steal-coin tracking + PlayerEntity.Zeny mutation hook.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
