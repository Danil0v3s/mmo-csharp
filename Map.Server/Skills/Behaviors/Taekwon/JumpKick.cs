using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// TK_JUMPKICK — auto-generated stub from
/// <c>src/map/skills/taekwon/jumpkick.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class JumpKick : SkillImpl
{
    public JumpKick() : base(SkillIds.TK_JUMPKICK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *sc = status_get_sc(src);
    // 	// Different damage formulas depending on damage trigger
    // 	if (sc && sc->getSCE(SC_COMBO) && sc->getSCE(SC_COMBO)->val1 == getSkillId())
    // 		base_skillratio += -100 + 4 * status_get_lv(src); // Tumble formula [4%*baselevel]
    // 	else if (wd->miscflag) {
    // 		base_skillratio += -100 + 4 * status_get_lv(src); // Running formula [4%*baselevel]
    // 		if (sc && sc->getSCE(SC_SPURT)) // Spurt formula [8%*baselevel]
    // 			base_skillratio *= 2;
    // 	} else
    // 		base_skillratio += -70 + 10 * skill_lv;
    return baseRatio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *tsc = status_get_sc(target);
    // 	map_session_data *dstsd = BL_CAST(BL_PC, target);
    // 
    // 	// debuff the following statuses
    // 	if (dstsd && dstsd->class_ != MAPID_SOUL_LINKER && tsc != nullptr && !tsc->getSCE(SC_PRESERVE)) {
    // 		status_change_end(target, SC_SPIRIT);
    // 		status_change_end(target, SC_ADRENALINE2);
    // 		status_change_end(target, SC_KAITE);
    // 		status_change_end(target, SC_KAAHI);
    // 		status_change_end(target, SC_ONEHAND);
    // 		status_change_end(target, SC_ASPDPOTION2);
    // 		// New soul links confirmed to not dispell with this skill
    // 		// but thats likely a bug since soul links can't stack and
    // 		// soul cutter skill works on them. So ill add this here for now. [Rytech]
    // 		status_change_end(target, SC_SOULGOLEM);
    // 		status_change_end(target, SC_SOULSHADOW);
    // 		status_change_end(target, SC_SOULFALCON);
    // 		status_change_end(target, SC_SOULFAIRY);
    // 	}
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data *sd = BL_CAST(BL_PC, src);
    // 
    // 	/* Check if the target is an enemy; if not, skill should fail so the character doesn't unit_movepos (exploitable) */
    // 	if (battle_check_target(src, target, BCT_ENEMY) > 0) {
    // 		if (unit_movepos(src, target->x, target->y, 2, 1)) {
    // 			skill_attack(BF_WEAPON, src, src, target, getSkillId(), skill_lv, tick, flag);
    // 			clif_blown(src);
    // 		}
    // 	} else if (sd) {
    // 		clif_skill_fail(*sd, getSkillId(), USESKILL_FAIL);
    // 	}
    }
}
