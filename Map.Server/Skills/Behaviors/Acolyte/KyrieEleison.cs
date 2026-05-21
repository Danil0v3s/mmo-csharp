using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// PR_KYRIE — Priest Kyrie Eleison. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/kyrieeleison.cpp</c>.
///
/// <para>Applies <see cref="StatusType.Kyrie"/> — a damage-absorb
/// shield with a max-hit + max-damage counter. The shield's
/// thresholds live on the SC handler (which the combat-side
/// DamageService consumer reads to apply absorption).</para>
///
/// <para>Duration: <c>120 000 ms</c> per <c>db/re/skill_db.yml</c>.</para>
/// </summary>
public sealed class KyrieEleison : SkillImpl
{
    public KyrieEleison() : base(SkillIds.PR_KYRIE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: clif_skill_nodamage(target, *target, getSkillId(), skill_lv,
        //          sc_start(src, target, type, 100, skill_lv, skill_get_time(...)));
        bool landed = ctx.Sc?.Start(target, StatusType.Kyrie,
            val1: skillLevel, 0, 0, 0, durationMs: 120_000, src) != null;
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel, landed);
    }
}
