using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_EL_ACTION — Sorcerer Elemental Action. Manual port of
/// <c>rathena-fork/src/map/skills/mage/elementalaction.cpp</c>.
///
/// <para>Tells the caster's bound elemental to attack the target.
/// rAthena routes through <c>elemental_action</c> + post-action
/// <c>skill_blockpc_start</c> (3 s / 6 s / 9 s based on elemental
/// tier). C# port is a stub broadcast — the elemental-AI hook + skill
/// block timer are not wired here yet.</para>
/// </summary>
public sealed class ElementalAction : SkillImpl
{
    public ElementalAction() : base(SkillIds.SO_EL_ACTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd) return;
        // Deferred: bound-elemental subsystem (elemental_action + skill_blockpc_start) isn't
        // ported yet — PlayerEntity has no BoundElemental field.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
