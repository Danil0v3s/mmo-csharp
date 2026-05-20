using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_ACIDIFIED_ZONE_WATER — auto-generated stub from
/// <c>src/map/skills/merchant/acidifiedzonewater.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class AcidifiedZoneWater : RecursiveDamageSplashSkillImpl
{
    public AcidifiedZoneWater() : base(SkillIds.BO_ACIDIFIED_ZONE_WATER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_data* tstatus = status_get_status_data(*target);
    // 	const status_change *sc = status_get_sc(src);
    // 
    // 	// All BO_ACIDIFIED_ZONE_* deal the same damage? [Rytech]
    // 	skillratio += -100 + 400 * skill_lv + 5 * sstatus->pow;
    // 
    // 	if( sc != nullptr && sc->getSCE( SC_RESEARCHREPORT ) ){
    // 		skillratio += skillratio * 50 / 100;
    // 
    // 		if (tstatus->race == RC_FORMLESS || tstatus->race == RC_PLANT)
    // 			skillratio += skillratio * 50 / 100;
    // 	}
    // 
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // flag|=1;//Set flag to 1 to prevent deleting ammo (it will be deleted on group-delete).
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    }

}
