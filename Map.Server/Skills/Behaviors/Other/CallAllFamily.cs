using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// WE_CALLALLFAMILY — Call All Family (skill.cpp:WE_CALLALLFAMILY arm).
/// Teleports the caster's partner + child to the caster's current
/// cell, using the canonical <c>pc_get_partner</c> /
/// <c>pc_get_child</c> lookups (<see cref="PlayerEntity.PartnerId"/>
/// / <see cref="PlayerEntity.ChildCharId"/>). Only family members
/// currently online on this map server are warpable; offline members
/// silently skip (rAthena does the same — the partner side has to be
/// logged in for the helper to find a session pointer).
/// </summary>
public sealed class CallAllFamily : SkillImpl
{
    public CallAllFamily() : base(SkillIds.WE_CALLALLFAMILY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        if (ctx.World == null || ctx.Setpos == null) return;
        var mapName = ResolveMapName(pc.MapId, ctx);
        if (mapName is null) return;
        if (pc.PartnerId != 0)
            WarpCharToCell(pc.PartnerId, mapName, pc.X, pc.Y, ctx);
        if (pc.ChildCharId != 0)
            WarpCharToCell(pc.ChildCharId, mapName, pc.X, pc.Y, ctx);
    }

    private static string? ResolveMapName(uint mapId, SkillBehaviorContext ctx)
    {
        if (ctx.World == null) return null;
        foreach (var m in ctx.World.All)
            if ((uint)m.Name.GetHashCode() == mapId) return m.Name;
        return null;
    }

    private static void WarpCharToCell(int charId, string mapName, short x, short y, SkillBehaviorContext ctx)
    {
        foreach (var e in ctx.Entities.All())
        {
            if (e is PlayerEntity p && p.CharacterId == charId)
            {
                ctx.Setpos?.Setpos(p, mapName, x, y);
                return;
            }
        }
    }
}
