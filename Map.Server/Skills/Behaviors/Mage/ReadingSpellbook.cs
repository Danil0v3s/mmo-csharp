using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_READING_SB_READING — Warlock Reading Spellbook. Consumes a
/// spellbook item and gives the caster the corresponding pre-cast
/// spell charge. Spellbook dispatch (skill_spellbook) is TODO until
/// the spellbook helper is wired. The base-skill prerequisite check
/// (pc_checkskill WL_READING_SB) is omitted — that skill isn't on
/// our SkillIds catalog.
/// </summary>
public sealed class ReadingSpellbook : SkillImpl
{
    public ReadingSpellbook() : base(SkillIds.WL_READING_SB_READING) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd) return;
        if (skillLevel < 1 || skillLevel > 10)
        {
            ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.Skill);
            return;
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: skill_spellbook(sd, ITEMID_WL_MB_SG + lv - 1) — wired with the spellbook service.
    }
}
