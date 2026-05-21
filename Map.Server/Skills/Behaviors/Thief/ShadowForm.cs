using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_SHADOWFORM — Shadow Form. Manual port of
/// <c>rathena-fork/src/map/skills/thief/shadowform.cpp</c>.
/// Casts SC__SHADOWFORM on the caster, linking the target's id and
/// granting 4 + skill_lv reflection charges. Linking-id wiring is TODO.
/// </summary>
public sealed class ShadowForm : SkillImpl
{
    public ShadowForm() : base(SkillIds.SC_SHADOWFORM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
