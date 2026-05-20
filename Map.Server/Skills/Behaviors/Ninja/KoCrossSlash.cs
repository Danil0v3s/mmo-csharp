using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_JYUMONJIKIRI — auto-generated stub from
/// <c>src/map/skills/ninja/kocrossslash.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class KoCrossSlash : WeaponSkillImpl
{
    public KoCrossSlash() : base(SkillIds.KO_JYUMONJIKIRI) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *tsc = status_get_sc(&target);
    // 
    // 	if (tsc != nullptr && tsc->hasSCE(SC_JYUMONJIKIRI))
    // 		dmg.div_ *= -1; // TODO: needs more info
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src,target,SC_JYUMONJIKIRI,100,skill_lv,skill_get_time(getSkillId(),skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *sc = status_get_sc(src);
    // 	const status_change *tsc = status_get_sc(target);
    // 
    // 	skillratio += -100 + 200 * skill_lv;
    // 	RE_LVL_DMOD(120);
    // 	if(tsc && tsc->getSCE(SC_JYUMONJIKIRI))
    // 		skillratio += skill_lv * status_get_lv(src);
    // 	if (sc && sc->getSCE(SC_KAGEMUSYA))
    // 		skillratio += skillratio * sc->getSCE(SC_KAGEMUSYA)->val2 / 100;
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int16 x, y;
    // 	int16 dir = map_calc_dir(src,target->x,target->y);
    // 
    // 	if (dir > 0 && dir < 4)
    // 		x = 2;
    // 	else if (dir > 4)
    // 		x = -2;
    // 	else
    // 		x = 0;
    // 	if (dir > 2 && dir < 6)
    // 		y = 2;
    // 	else if (dir == 7 || dir < 2)
    // 		y = -2;
    // 	else
    // 		y = 0;
    // 	if (unit_movepos(src,target->x + x,target->y + y,1,1)) {
    // 		clif_blown(src);
    // 		WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    // 	}
    }
}
