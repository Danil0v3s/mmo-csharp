using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_HELLJUDGEMENT2 — auto-generated stub from
/// <c>src/map/skills/npc/hellsjudgement2.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class HellsJudgement2 : RecursiveDamageSplashSkillImpl
{
    public HellsJudgement2() : base(SkillIds.NPC_HELLJUDGEMENT2) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // switch(rnd()%6) {
    // 	case 0:
    // 		sc_start(src,target,SC_SLEEP,100,skill_lv,skill_get_time2(getSkillId(),skill_lv));
    // 		break;
    // 	case 1:
    // 		sc_start(src,target,SC_CONFUSION,100,skill_lv,skill_get_time2(getSkillId(),skill_lv));
    // 		break;
    // 	case 2:
    // 		sc_start(src,target,SC_HALLUCINATION,100,skill_lv,skill_get_time2(getSkillId(),skill_lv));
    // 		break;
    // 	case 3:
    // 		sc_start(src,target,SC_STUN,100,skill_lv,skill_get_time2(getSkillId(),skill_lv));
    // 		break;
    // 	case 4:
    // 		sc_start(src,target,SC_FEAR,100,skill_lv,skill_get_time2(getSkillId(),skill_lv));
    // 		break;
    // 	default:
    // 		sc_start(src,target,SC_CURSE,100,skill_lv,skill_get_time2(getSkillId(),skill_lv));
    // 		break;
    // 	}
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // base_skillratio += 100 * (skill_lv - 1);
    return baseRatio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_castend_damage_id(src, src, getSkillId(), skill_lv, tick, flag);
    }
}
