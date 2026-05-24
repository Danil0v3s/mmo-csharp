using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_WOODENWARRIOR — Biolo Wooden Warrior
/// (skill.cpp:BO_WOODENWARRIOR). Starts <c>SC_BIONIC_WOODENWARRIOR</c>
/// and spawns <see cref="MobIds.BionicWoodenWarrior"/> at the caster's
/// cell with AI_BIONIC + master link + 60 s lifetime.
/// </summary>
public sealed class WoodenWarrior : SkillImpl
{
    private const int LifetimeMs = 60_000;

    public WoodenWarrior() : base(SkillIds.BO_WOODENWARRIOR) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(src, StatusType.BionicWoodenwarrior, val1: skillLevel, 0, 0, 0, durationMs: LifetimeMs, src);
        ctx.MobSpawn?.SpawnWithAi(src.Id, src.MapId, MobIds.BionicWoodenWarrior, src.X, src.Y,
            MobSpecialAi.Bionic, LifetimeMs);
    }
}
