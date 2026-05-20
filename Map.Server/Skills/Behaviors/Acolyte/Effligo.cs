using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CD_EFFLIGO — auto-generated stub from
/// <c>src/map/skills/acolyte/effligo.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Effligo : WeaponSkillImpl
{
    public Effligo() : base(SkillIds.CD_EFFLIGO) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 
    // 	WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 	const status_data* tstatus = status_get_status_data(*target);
    // 
    // 	skillratio += -100 + 1650 * skill_lv + 7 * sstatus->pow;
    // 	skillratio += 8 * pc_checkskill(sd, CD_MACE_BOOK_M);
    // 	if (tstatus->race == RC_UNDEAD || tstatus->race == RC_DEMON) {
    // 		skillratio += 150 * skill_lv;
    // 		skillratio += 7 * pc_checkskill(sd, CD_MACE_BOOK_M);
    // 	}
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
