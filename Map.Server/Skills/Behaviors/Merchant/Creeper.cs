using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_CREEPER — Biolo Creeper (bionic). Manual port of
/// <c>rathena-fork/src/map/skills/merchant/creeper.cpp</c>.
/// Applies SC_BIONIC_CREEPER and spawns <see cref="MobIds.BionicCreeper"/>
/// at the caster's cell with AI_BIONIC + master link + 60 s lifetime.
/// </summary>
public sealed class Creeper : SkillImpl
{
    private const int LifetimeMs = 60_000;

    public Creeper() : base(SkillIds.BO_CREEPER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.BionicCreeper, val1: skillLevel, 0, 0, 0, durationMs: LifetimeMs, src);
        ctx.MobSpawn?.SpawnWithAi(src.Id, src.MapId, MobIds.BionicCreeper, src.X, src.Y,
            MobSpecialAi.Bionic, LifetimeMs);
    }
}
