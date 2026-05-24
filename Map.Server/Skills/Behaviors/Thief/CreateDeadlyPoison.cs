using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ASC_CDP — Create Deadly Poison (skill.cpp:ASC_CDP arm). Produces
/// one Poison Bottle (item 678) via the rAthena produce_db.txt recipe
/// keyed on RequireSkill = ASC_CDP. The recipe service walks every
/// material in the recipe; consume + add happens through
/// <see cref="ISkillProductionService.ProduceMix"/>.
/// </summary>
public sealed class CreateDeadlyPoison : SkillImpl
{
    public CreateDeadlyPoison() : base(SkillIds.ASC_CDP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (ctx.Recipes == null || ctx.Production == null) return;
        var recipes = ctx.Recipes.GetByRequireSkill(SkillIds.ASC_CDP, (byte)skillLevel);
        if (recipes.Count == 0) return;
        // ASC_CDP has a single recipe (produce_db row 127 → Poison Bottle).
        ctx.Production.ProduceMix(pc, recipes[0].Id, qty: 1);
    }
}
