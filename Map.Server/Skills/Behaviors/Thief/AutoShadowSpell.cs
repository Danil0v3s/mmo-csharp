using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_AUTOSHADOWSPELL — Auto Shadow Spell. Manual port of
/// <c>rathena-fork/src/map/skills/thief/autoshadowspell.cpp</c>.
/// Opens the autocast list of reproduced / cloned skills. Skill-list
/// UI deferred — animation only when the player has a candidate skill.
/// </summary>
public sealed class AutoShadowSpell : SkillImpl
{
    public AutoShadowSpell() : base(SkillIds.SC_AUTOSHADOWSPELL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: clif_autoshadowspell_list(sd) — pops the spell-pick UI
        // listing the player's cloned skills (rAthena
        // <c>sd-&gt;cloneskill_idx</c>). The packet header isn't on
        // Core.Server/Packets/Out/ZC yet; once it lands the picker UI +
        // SC_STOP menu-lock fire here. The animation broadcast lands so
        // the cast frame plays correctly today.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
