using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_RUWACH — Acolyte Ruwach. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/ruwach.cpp</c>.
///
/// <para>Self-target buff that reveals hidden enemies in a 2-cell
/// splash radius. The fork's <c>sc_start2</c> attaches
/// <see cref="StatusType.Ruwach"/> with a second val carrying the
/// skill id (for unhide identification). Duration is the
/// skill_db ladder, fixed at 10 s for AL_RUWACH (MaxLevel = 1).</para>
///
/// <para>Also overrides <see cref="CalculateSkillRatio"/> to add
/// +45 % skill ratio — used when Ruwach lands its splash hit on
/// hidden enemies (Hide / Cloaking get pulsed out + take damage).</para>
/// </summary>
public sealed class Ruwach : SkillImpl
{
    public Ruwach() : base(SkillIds.AL_RUWACH) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: sc_start2(src, bl, type, 100, skill_lv, getSkillId(), skill_get_time(...));
        //   val1 = skillLevel
        //   val2 = skillId (used when the hidden detection fires to know which
        //          skill broke the hide)
        // 100% landing chance, 10s duration per skill_db.yml.
        bool landed = false;
        if (ctx.Sc != null)
        {
            var sc = ctx.Sc.Start(target, StatusType.Ruwach,
                val1: skillLevel, val2: SkillId, 0, 0,
                durationMs: 10_000, src);
            landed = sc != null;
        }

        // rAthena uses the bool result of sc_start2 as the broadcast's success
        // flag — same single-line idiom as DecreaseAgi.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, landed);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: base_skillratio += 45;
        return baseRatio + 45;
    }
}
