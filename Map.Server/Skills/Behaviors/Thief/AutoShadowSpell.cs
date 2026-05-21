using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_AUTOSHADOWSPELL — Auto Shadow Spell. Manual port of
/// <c>rathena-fork/src/map/skills/thief/autoshadowspell.cpp</c>.
/// Opens the autocast list of reproduced / cloned skills. Skill-list
/// UI is TODO — animation only when the player has a candidate skill.
/// </summary>
public sealed class AutoShadowSpell : SkillImpl
{
    public AutoShadowSpell() : base(SkillIds.SC_AUTOSHADOWSPELL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: clif_autoshadowspell_list dialog + SC_STOP for menu lock.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
