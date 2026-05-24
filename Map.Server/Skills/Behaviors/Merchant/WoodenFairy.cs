using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_WOODEN_FAIRY — Biolo Wooden Fairy. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/woodenfairy.cpp</c>.
/// Starts SC_BIONIC_WOODEN_FAIRY and spawns MOBID_BIONIC_WOODEN_FAIRY
/// at the caster's cell with AI_BIONIC. Bionic mob spawn is TODO.
/// </summary>
public sealed class WoodenFairy : SkillImpl
{
    public WoodenFairy() : base(SkillIds.BO_WOODEN_FAIRY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(src, StatusType.BionicWoodenFairy, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        // Deferred: MOBID_BIONIC_WOODEN_FAIRY mob id + AI_BIONIC binding +
        // master-id linkage + delete-timer aren't surfaced through
        // IMobSpawnService.SpawnAt — would create wild mobs otherwise.
    }
}
