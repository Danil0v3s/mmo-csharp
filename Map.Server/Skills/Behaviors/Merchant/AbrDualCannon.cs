using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_SUMMON_ABR_DUAL_CANNON — Meister Summon ABR Dual Cannon. Manual
/// port of <c>rathena-fork/src/map/skills/merchant/abrdualcannon.cpp</c>.
/// Spawns <see cref="MobIds.AbrDualCannon"/> at the caster's cell with
/// AI_ABR + master link + 60 s lifetime; applies SC_ABR_DUAL_CANNON.
/// </summary>
public sealed class AbrDualCannon : SkillImpl
{
    private const int LifetimeMs = 60_000;

    public AbrDualCannon() : base(SkillIds.MT_SUMMON_ABR_DUAL_CANNON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.AbrDualCannon, val1: skillLevel, 0, 0, 0, durationMs: LifetimeMs, src);
        ctx.MobSpawn?.SpawnWithAi(src.Id, src.MapId, MobIds.AbrDualCannon, src.X, src.Y,
            MobSpecialAi.Abr, LifetimeMs);
    }
}
