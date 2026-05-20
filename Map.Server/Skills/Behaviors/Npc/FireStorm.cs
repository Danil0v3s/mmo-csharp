using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_FIRESTORM — auto-generated stub from
/// <c>src/map/skills/npc/firestorm.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class FireStorm : SkillImpl
{
    public FireStorm() : base(SkillIds.NPC_FIRESTORM) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src,target,SC_BURNT,100,skill_lv,skill_get_time(getSkillId(),skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // base_skillratio += 200;
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_attack(BF_MAGIC, src, src, target, getSkillId(), skill_lv, tick, flag);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int32 sflag = flag;
    // 
    // 	if( skill_lv > 1 )
    // 		sflag |= 4;
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	map_foreachinshootrange(skill_area_sub,src,skill_get_splash(getSkillId(),skill_lv),splash_target(src),src,
    // 		getSkillId(),skill_lv,tick,sflag|BCT_ENEMY|SD_ANIMATION|1,skill_castend_damage_id);
    }
}
