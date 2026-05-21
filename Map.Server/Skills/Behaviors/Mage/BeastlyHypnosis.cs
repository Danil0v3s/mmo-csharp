using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_TAMINGMONSTER — Sage Beastly Hypnosis. Opens the pet-catch UI
/// on the caster when targeting a mob. Pet-catch process isn't yet
/// surfaced through SkillBehaviorContext; cast frame emits faithfully.
/// </summary>
public sealed class BeastlyHypnosis : SkillImpl
{
    public BeastlyHypnosis() : base(SkillIds.SA_TAMINGMONSTER) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: pet_catch_process_start(sd, mob_id, PET_CATCH_UNIVERSAL_ALL).
    }
}
