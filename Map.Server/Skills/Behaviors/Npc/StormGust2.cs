using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_STORMGUST2 — auto-generated stub from
/// <c>src/map/skills/npc/stormgust2.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class StormGust2 : SkillImpl
{
    public StormGust2() : base(SkillIds.NPC_STORMGUST2) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (skill_lv == 1)
    // 		sc_start(src,target,SC_FREEZE,10,skill_lv,skill_get_time2(getSkillId(),skill_lv));
    // 	else if (skill_lv == 2)
    // 		sc_start(src,target,SC_FREEZE,7,skill_lv,skill_get_time2(getSkillId(),skill_lv));
    // 	else
    // 		sc_start(src,target,SC_FREEZE,3,skill_lv,skill_get_time2(getSkillId(),skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // base_skillratio += 200 * skill_lv;
    return baseRatio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // flag|=1; // Set flag to 1 to prevent deleting ammo (it will be deleted on group-delete).
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    }
}
