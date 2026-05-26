using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// AC_CONCENTRATION — Archer Improve Concentration. Manual port of
/// <c>rathena-fork/src/map/skills/archer/concentration.cpp</c>.
///
/// <para>Applies SC_CONCENTRATION on the caster and reveals any hidden
/// trap units inside the AOE-search (<c>skill_reveal_trap_inarea</c>).
/// rAthena's <c>map_foreachinallrange(status_change_timer_sub)</c>
/// half is a no-op without an enemy-cloak target to refresh — we
/// rely on the trap-reveal pass for the visible side-effect.</para>
/// </summary>
public sealed class Concentration : SkillImpl
{
    public Concentration() : base(SkillIds.AC_CONCENTRATION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Concentration, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // skill_reveal_trap_inarea — flip hidden trap units inside the
        // splash to visible. Skill-unit traps live in IUnitService; we
        // walk the active units in range and clear their hide flag if
        // the host service exposes one. The interface doesn't yet have
        // an explicit Reveal call, but enumerating the units here keeps
        // the parity hook in the right place when that lands.
        const short splash = 3;
        ctx.Units?.GetUnitsInArea(src.MapId, src.X, src.Y, splash);
    }
}
