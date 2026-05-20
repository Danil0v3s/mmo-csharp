using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_RAINOFMETEOR — auto-generated stub from
/// <c>src/map/skills/npc/rainofmeteor.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class RainOfMeteor : SkillImpl
{
    public RainOfMeteor() : base(SkillIds.NPC_RAINOFMETEOR) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // base_skillratio += 350;	// unknown ratio
    return baseRatio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int32 area = skill_get_splash(getSkillId(), skill_lv);
    // 	int16 tmpx = 0;
    // 	int16 tmpy = 0;
    // 
    // 	for (int32 i = 1; i <= (skill_get_time(getSkillId(), skill_lv)/skill_get_unit_interval(getSkillId())); i++) {
    // 		// Casts a double meteor in the first interval.
    // 		if (i == 1) {
    // 			// The first meteor is at the center
    // 			skill_unitsetting(src, getSkillId(), skill_lv, x, y, flag+skill_get_unit_interval(getSkillId()));
    // 
    // 			// The second meteor is near the first
    // 			tmpx = x - 1 + rnd()%3;
    // 			tmpy = y - 1 + rnd()%3;
    // 			skill_unitsetting(src, getSkillId(), skill_lv, tmpx, tmpy, flag+skill_get_unit_interval(getSkillId()));
    // 		}
    // 		else {	// Casts 1 meteor per interval in the splash area
    // 			tmpx = x - area + rnd()%(area * 2 + 1);
    // 			tmpy = y - area + rnd()%(area * 2 + 1);
    // 			skill_unitsetting(src, getSkillId(), skill_lv, tmpx, tmpy, flag+i*skill_get_unit_interval(getSkillId()));
    // 		}
    // 	}
    }
}
