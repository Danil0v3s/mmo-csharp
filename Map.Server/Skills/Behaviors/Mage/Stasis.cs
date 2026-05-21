using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>WL_STASIS — Warlock Stasis. AoE around caster silencing magic casts.</summary>
public sealed class Stasis : SkillImpl
{
    public Stasis() : base(SkillIds.WL_STASIS) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        const short splash = 7;
        var victims = ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y, splash,
            EntityType.Mob | EntityType.Pc).Where(v => v.Id != src.Id);
        foreach (var v in victims)
            ctx.Sc?.Start(v, StatusType.Stasis, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
