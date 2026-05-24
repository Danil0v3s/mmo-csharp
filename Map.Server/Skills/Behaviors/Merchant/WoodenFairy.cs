using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_WOODEN_FAIRY — Biolo Wooden Fairy (skill.cpp:BO_WOODEN_FAIRY).
/// Starts <c>SC_BIONIC_WOODEN_FAIRY</c> and spawns
/// <see cref="MobIds.BionicWoodenFairy"/> at the caster's cell with
/// AI_BIONIC + master link + 60 s lifetime cap via
/// <see cref="IMobSpawnService.SpawnWithAi"/>.
/// </summary>
public sealed class WoodenFairy : SkillImpl
{
    private const int LifetimeMs = 60_000;

    public WoodenFairy() : base(SkillIds.BO_WOODEN_FAIRY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(src, StatusType.BionicWoodenFairy, val1: skillLevel, 0, 0, 0, durationMs: LifetimeMs, src);
        ctx.MobSpawn?.SpawnWithAi(src.Id, src.MapId, MobIds.BionicWoodenFairy, src.X, src.Y,
            MobSpecialAi.Bionic, LifetimeMs);
    }
}
