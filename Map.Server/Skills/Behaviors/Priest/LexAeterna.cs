using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Priest;

/// <summary>
/// PR_LEXAETERNA — Priest Lex Aeterna. Mirrors
/// <c>rathena-fork/src/map/skills/priest/lexaeterna.cpp</c>.
///
/// Apply <see cref="StatusType.Aeterna"/> on target — next physical
/// or magic hit deals double damage, then SC ends. Permanent until
/// consumed (rAthena duration = −1). Refuses re-cast.
/// </summary>
public sealed class LexAeterna : SkillImpl
{
    public LexAeterna() : base(SkillIds.PR_LEXAETERNA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return;
        // Refuse re-cast — only one Aeterna may live on the target.
        if (ctx.Sc.Get(target, StatusType.Aeterna) != null) return;
        ctx.Sc.Start(target, StatusType.Aeterna, val1: skillLevel, 0, 0, 0,
            durationMs: -1, src);
    }
}
