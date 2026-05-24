using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_CREATENEWPOISON — Create New Poison (skill.cpp:GC_CREATENEWPOISON
/// arm). Opens the produce-mix list with subtype 25 (the GC poison
/// catalog). The user's pick consumes the recipe ingredients and adds
/// the chosen poison to inventory via the produce path.
/// </summary>
public sealed class CreateNewPoison : SkillImpl
{
    public CreateNewPoison() : base(SkillIds.GC_CREATENEWPOISON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // rAthena lists the poisons GC_CREATENEWPOISON can produce
        // (Paralyse / Leech End / Oblivion Curse / Death Hurt / Toxin /
        // Pyrexia / Magic Mushroom / Venom Bleed); we forward an empty
        // list until the produce_db.yml catalog populates them.
        ctx.Client?.BroadcastProduceMixList(pc, produceType: 25, craftableItemIds: Array.Empty<ushort>());
    }
}
