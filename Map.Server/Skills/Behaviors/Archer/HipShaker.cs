using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// DC_UGLYDANCE — auto-generated stub from
/// <c>src/map/skills/archer/hipshaker.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class HipShaker : SkillImpl
{
    public HipShaker() : base(SkillIds.DC_UGLYDANCE) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	// !TODO: How does caster's DEX/AGI play a role?
    // 	status_zap( target, 0, 2 * skill_lv + 10 );
    // #else
    // 	map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	int32 rate = 5 + 5 * skill_lv;
    // 	rate += skill_lv * pc_checkskill(sd, DC_DANCINGLESSON);
    // 	status_zap( target, 0, rate );
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
    // 	// Ammo should be deleted right away.
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    // #endif
    }
}
