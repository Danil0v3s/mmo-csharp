using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_SOLID_SKIN — Elemental Solid Skin. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/solidskin.cpp</c>.
/// Toggles SC_SOLID_SKIN on target + SC_SOLID_SKIN_OPTION on the
/// elemental.
/// </summary>
public sealed class SolidSkin : SkillImpl
{
    public SolidSkin() : base(SkillIds.EL_SOLID_SKIN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.SolidSkin) != null
            || ctx.Sc?.Get(src, StatusType.SolidSkinOption) != null)
        {
            ctx.Sc.End(target, StatusType.SolidSkin);
            ctx.Sc.End(src, StatusType.SolidSkinOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.SolidSkinOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.SolidSkin, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
