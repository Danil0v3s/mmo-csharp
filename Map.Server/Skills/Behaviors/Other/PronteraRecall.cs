using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_PRONTERA_RECALL — Prontera recall. Manual port of
/// <c>rathena-fork/src/map/skills/other/pronterarecall.cpp</c>.
/// Lv 1 → (115, 72); Lv 2 → (159, 192) on prontera.
/// </summary>
public sealed class PronteraRecall : SkillImpl
{
    public PronteraRecall() : base(SkillIds.ALL_PRONTERA_RECALL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var (x, y) = skillLevel == 1 ? ((short)115, (short)72) : ((short)159, (short)192);
        ctx.Setpos?.Setpos(pc, "prontera", x, y);
    }
}
