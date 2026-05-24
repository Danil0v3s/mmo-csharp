using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_BUNSINJYUTSU — Mirror Image. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/mirrorimage.cpp</c>.
/// Recasting cancels existing image; also ends SC_NEN.
/// </summary>
public sealed class MirrorImage : StatusSkillImpl
{
    public MirrorImage() : base(SkillIds.NJ_BUNSINJYUTSU) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: recasting cancels existing mirror image.
        ctx.Sc?.End(target, StatusType.Bunsinjyutsu);
        base.CastendNoDamageId(src, target, skillLevel, ctx);
        ctx.Sc?.End(target, StatusType.Nen);
    }
}
