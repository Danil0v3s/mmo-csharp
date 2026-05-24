using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_WOODENWARRIOR — Biolo Wooden Warrior. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/woodenwarrior.cpp</c>.
/// Starts SC_BIONIC_WOODENWARRIOR and spawns MOBID_BIONIC_WOODENWARRIOR
/// at the caster's cell with AI_BIONIC. Bionic mob spawn is TODO.
/// </summary>
public sealed class WoodenWarrior : SkillImpl
{
    public WoodenWarrior() : base(SkillIds.BO_WOODENWARRIOR) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(src, StatusType.BionicWoodenwarrior, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        // Deferred: MOBID_BIONIC_WOODENWARRIOR + AI_BIONIC + master-id linkage
        // + delete-timer aren't surfaced through IMobSpawnService.SpawnAt.
    }
}
