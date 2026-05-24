using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BS_GREED — Blacksmith Greed (skill.cpp:BS_GREED arm). Auto-loots
/// every floor item inside a 3×3 splash around the caster. rAthena
/// dispatches via <c>map_foreachinrange(skill_greed, BL_ITEM, …)</c>
/// → <c>pc_takeitem</c>; we walk <see cref="EntityType.Item"/> via
/// <see cref="IEntityRegistry.ForEachInRange"/> and call
/// <see cref="Map.Server.Items.IItemDropService.TryPickup"/> on each.
/// </summary>
public sealed class Greed : SkillImpl
{
    /// <summary>3×3 pickup range (rAthena BS_GREED splash).</summary>
    private const short SplashRadius = 1;

    public Greed() : base(SkillIds.BS_GREED) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (ctx.Drops == null) return;
        var floor = ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y, SplashRadius, EntityType.Item);
        foreach (var f in floor)
        {
            // Failure cases (full inventory, owner-protection window,
            // drop-cooldown) are silent — matches rAthena skill_greed.
            ctx.Drops.TryPickup(pc, f.Id, out _);
        }
    }
}
