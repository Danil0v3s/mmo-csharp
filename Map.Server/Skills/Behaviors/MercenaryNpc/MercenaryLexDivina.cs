using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_LEXDIVINA — Mercenary Lex Divina. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_lexdivina.cpp</c>.
/// If the target is already silenced, the SC is removed; otherwise the
/// silence is scheduled via timer-skill in +1000 ms. Timer scheduling
/// is simplified to immediate apply here.
/// </summary>
public sealed class MercenaryLexDivina : SkillImpl
{
    public MercenaryLexDivina() : base(SkillIds.MER_LEXDIVINA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Silence) != null)
            ctx.Sc.End(target, StatusType.Silence);
        else
            ctx.Sc?.Start(target, StatusType.Silence, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
