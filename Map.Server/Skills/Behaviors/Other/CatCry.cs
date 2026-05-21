using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_CATCRY — All Cat Cry emote. Manual port of
/// <c>rathena-fork/src/map/skills/other/catcry.cpp</c>. No-op other than
/// the animation.
/// </summary>
public sealed class CatCry : SkillImpl
{
    public CatCry() : base(SkillIds.ALL_CATCRY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
