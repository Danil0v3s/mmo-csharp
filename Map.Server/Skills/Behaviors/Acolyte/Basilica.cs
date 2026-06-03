using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// HP_BASILICA — High Priest Basilica (renewal). Applies <see cref="StatusType.Basilica"/>
/// to the caster (TargetType Self).
///
/// <para><b>COMBAT-68 — renewal vs pre-renewal.</b> Renewal Basilica is <i>not</i> a
/// damage-immune sanctuary. In rAthena renewal HP_BASILICA places <b>no ground unit</b>,
/// marks <b>no CELL_BASILICA</b> cells (skill.cpp:21830 is <c>#ifndef RENEWAL</c>), and so
/// <b>never applies SC_BASILICA_CELL</b> (<c>pc_cell_basilica</c> only fires off a marked
/// cell, which renewal never marks). The renewal effect is the self-buff <c>SC_BASILICA</c>
/// (status.yml): an offensive element buff (weapon <c>addele[Dark/Undead] += val1*5</c>, magic
/// <c>addele[Holy] += val1*3</c>; status.cpp:4768) plus the <c>NoAttack</c> caster state. Those
/// two effects are the real renewal gap — ➡️ COMBAT-87 (they need an SC→element-fold recalc seam
/// + a NoAttack gate, neither of which exists yet). The pre-renewal PVP-block sanctuary (unit +
/// CELL_BASILICA + SC_BASILICA_CELL, the mechanism COMBAT-49 guards) is out of scope — this
/// codebase targets renewal, where that SC is never applied.</para>
/// </summary>
public sealed class Basilica : StatusSkillImpl
{
    public Basilica() : base(SkillIds.HP_BASILICA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Renewal: apply the self-buff SC. Duration 30000+30000*lv (skill_db Duration1:
        // 60s/90s/120s/150s/180s for lv1-5).
        ctx.Sc?.Start(target, StatusType.Basilica,
            val1: skillLevel, 0, 0, 0, durationMs: 30_000 + 30_000 * skillLevel, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // No ground unit in renewal (skill.cpp HP_BASILICA unit/CELL_BASILICA paths are all
        // #ifndef RENEWAL). The self-buff in CastendNoDamageId carries the renewal effect.
    }
}
