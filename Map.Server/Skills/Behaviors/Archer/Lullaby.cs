using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BD_LULLABY — auto-generated stub from
/// <c>src/map/skills/archer/lullaby.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Lullaby : SkillImpl
{
    public Lullaby() : base(SkillIds.BD_LULLABY) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifndef RENEWAL
    // 	status_change *sc = status_get_sc(src);
    // 	status_data* sstatus = status_get_status_data(*src);
    // 
    // 	if (sc != nullptr && sc->getSCE(SC_DANCING) != nullptr) {
    // 		block_list* partner = map_id2bl(sc->getSCE(SC_DANCING)->val4);
    // 		if (partner == nullptr)
    // 			return;
    // 		status_data* pstatus = status_get_status_data(*partner);
    // 		if (pstatus == nullptr)
    // 			return;
    // 		status_change_start(src, target, skill_get_sc(getSkillId()), (sstatus->int_ + pstatus->int_ + rnd_value(100, 300)) * 10, skill_lv, 0, 0, 0, skill_get_time2(getSkillId(), skill_lv), SCSTART_NONE);
    // 	}
    // #else
    // 	// In renewal the chance is simply 100% and uses the original song duration as sleep duration
    // 	sc_start(src, target, skill_get_sc(getSkillId()), 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // #endif
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	skill_castend_song(src, getSkillId(), skill_lv, tick);
    // #endif
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifndef RENEWAL
    // 	flag|=1;//Set flag to 1 to prevent deleting ammo (it will be deleted on group-delete).
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    // #endif
    }
}
