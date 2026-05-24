using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_RELEASE — Warlock Release. Manual port of
/// <c>rathena-fork/src/map/skills/mage/release.cpp</c>.
///
/// <para>Two modes: lv 1 releases a pre-cast spell from Reading
/// Spellbook (SC_FREEZE_SP queue), lv 2 detonates summoned Spirit
/// Balls (SC_SPHERE_*). Both paths depend on Warlock-specific SCs
/// (FREEZE_SP, SPELLBOOK1..MAXSPELLBOOK, SPHERE_1..SPHERE_5) and
/// helpers (skill_check_condition_castbegin, skill_consume_requirement,
/// skill_addtimerskill with per-element ATK) that aren't wired here
/// yet — the whole body stays as TODO.</para>
/// </summary>
public sealed class Release : SkillImpl
{
    public Release() : base(SkillIds.WL_RELEASE) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity)
            return;
        // Deferred: spellbook detonation (lv 1) drains SC_FREEZE_SP / SC_SPELLBOOK1..7 stack,
        // summoned-ball release (lv 2) consumes SC_SPHERE_1..5. Both SC families + the
        // skill_addtimerskill per-element ATK helper aren't wired here yet.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, 0);
    }
}
