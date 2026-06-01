using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// TF_PICKSTONE — Find Stone. Manual port of
/// <c>rathena-fork/src/map/skills/thief/findstone.cpp</c>.
/// Grants 1× Stone (ITEMID_STONE = 7049) via
/// <see cref="Inventory.IInventoryService.GiveItem"/> when the caster
/// is a player. The session bridge resolves PlayerEntity →
/// MapSessionData; if either Sessions or Inventory is unwired (e.g.
/// in unit tests) only the animation broadcast lands.
/// </summary>
public sealed class FindStone : SkillImpl
{
    /// <summary>rAthena <c>ITEMID_STONE</c> — the item id Find Stone
    /// produces.</summary>
    private const uint ITEMID_STONE = 7049;

    public FindStone() : base(SkillIds.TF_PICKSTONE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is PlayerEntity pc && ctx.Sessions != null && ctx.Inventory != null)
        {
            var session = ctx.Sessions.TryGet(pc);
            if (session != null)
                ctx.Inventory.GiveItem(session, ITEMID_STONE, 1);
        }
    }
}
