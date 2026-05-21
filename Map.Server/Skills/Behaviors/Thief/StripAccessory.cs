using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_STRIPACCESSARY — Strip Accessory. Manual port of
/// <c>rathena-fork/src/map/skills/thief/stripaccessory.cpp</c>.
/// Strips the target's accessory. Strip service is TODO.
/// </summary>
public sealed class StripAccessory : SkillImpl
{
    public StripAccessory() : base(SkillIds.SC_STRIPACCESSARY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
