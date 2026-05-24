using Map.Server.Entities;
using Map.Server.Mob;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_SPHEREMINE — Summon Marine Sphere (skill.cpp:AM_SPHEREMINE).
/// Spawns <see cref="MobIds.MarineSphere"/> at the cast cell with
/// AI_SPHERE + master link + 30 s lifetime. The sphere AI handles
/// the auto-detonate on contact branch.
/// </summary>
public sealed class SummonMarineSphere : SkillImpl
{
    private const int LifetimeMs = 30_000;

    public SummonMarineSphere() : base(SkillIds.AM_SPHEREMINE) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        ctx.MobSpawn?.SpawnWithAi(src.Id, src.MapId, MobIds.MarineSphere, x, y,
            MobSpecialAi.Sphere, LifetimeMs);
    }
}
