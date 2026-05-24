using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_WUGRIDER — Ranger Warg Rider. rAthena
/// (<c>skill.cpp:13134</c>): if currently mounted on the warg, dismounts
/// (clear <c>OPTION_WUGRIDER</c>, restore <c>OPTION_WUG</c>). Otherwise
/// mounts (clear <c>OPTION_WUG</c>, set <c>OPTION_WUGRIDER</c>) so the
/// caster's sprite becomes the rider sprite. Requires the warg sprite
/// to exist (RA_WUGMASTERY having been toggled on) to mount.
/// </summary>
public sealed class WargRider : SkillImpl
{
    public WargRider() : base(SkillIds.RA_WUGRIDER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        var hasRider = (pc.Option & PlayerOption.Wugrider) != 0;
        var hasWug = (pc.Option & PlayerOption.Wug) != 0;
        if (hasRider)
        {
            ctx.Options?.SetWugRider(pc, false);
        }
        else if (hasWug)
        {
            ctx.Options?.SetWugRider(pc, true);
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
