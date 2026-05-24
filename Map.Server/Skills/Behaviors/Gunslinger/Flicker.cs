using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_FLICKER — Rebellion Flicker. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/flicker.cpp</c>.
/// Detonates Bind Traps and Howling Mines in range. Skill-tree query +
/// trap detonation pipeline deferred; we animate.
/// </summary>
public sealed class Flicker : SkillImpl
{
    public Flicker() : base(SkillIds.RL_FLICKER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // Deferred: enumerate own RL_B_TRAP units (skill_bind_trap) and RL_H_MINE
        // splash hits, then clear them — needs BL_SKILL splash enumerator.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
