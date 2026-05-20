using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_LIGHTNING_LAND — auto-generated stub from
/// <c>src/map/skills/mage/lightningland.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class LightningLand : SkillImpl
{
    public LightningLand() : base(SkillIds.EM_LIGHTNING_LAND) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src, target, SC_HANDICAPSTATE_LIGHTNINGSTRIKE, 3, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_change *sc = status_get_sc(src);
    // 
    // 	skillratio += -100 + 700 + 1100 * skill_lv;
    // 	skillratio += 5 * sstatus->spl;
    // 
    // 	if( sc != nullptr && sc->getSCE( SC_SUMMON_ELEMENTAL_PROCELLA ) ){
    // 		skillratio += 200 * skill_lv;
    // 		skillratio += 2 * sstatus->spl;
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
