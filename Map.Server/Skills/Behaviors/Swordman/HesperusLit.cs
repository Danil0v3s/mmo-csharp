using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_HESPERUSLIT — auto-generated stub from
/// <c>src/map/skills/swordman/hesperuslit.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class HesperusLit : WeaponSkillImpl
{
    public HesperusLit() : base(SkillIds.LG_HESPERUSLIT) { }

    public override void ApplyCounterAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sd == nullptr) {
    // 		return;
    // 	}
    // 
    // 	status_change_entry* sce = sd->sc.getSCE(SC_FORCEOFVANGUARD);
    // 
    // 	if (sce == nullptr) {
    // 		return;
    // 	}
    // 
    // 	for (int32 i = 0; i < sce->val3; i++) {
    // 		pc_addspiritball(sd, skill_get_time(LG_FORCEOFVANGUARD, 1), sce->val3);
    // 	}
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change* sc = status_get_sc(src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	if (sc && sc->getSCE(SC_INSPIRATION))
    // 		skillratio += -100 + 450 * skill_lv;
    // 	else
    // 		skillratio += -100 + 300 * skill_lv;
    // 	skillratio += sstatus->vit / 6; // !TODO: What's the VIT bonus?
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	status_change* sc = status_get_sc(src);
    // 
    // 	if( pc_checkskill(sd,LG_PINPOINTATTACK) > 0 && sc && sc->getSCE(SC_BANDING) && sc->getSCE(SC_BANDING)->val2 > 5 )
    // 		skill_castend_damage_id(src,target,LG_PINPOINTATTACK, rnd_value<uint16>(1, pc_checkskill(sd,LG_PINPOINTATTACK)),tick,0);
    }
}
