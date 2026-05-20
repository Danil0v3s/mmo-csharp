using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// PR_ASPERSIO — auto-generated stub from
/// <c>src/map/skills/acolyte/aspersio.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Aspersio : SkillImpl
{
    public Aspersio() : base(SkillIds.PR_ASPERSIO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	mob_data* dstmd = BL_CAST(BL_MOB, target);
    // 
    // 	if (sd && dstmd) {
    // 		clif_skill_nodamage(src,*target,getSkillId(), skill_lv, false);
    // 		return;
    // 	}
    // 	clif_skill_nodamage(src,*target, getSkillId(),skill_lv,
    // 		sc_start(src,target,skill_get_sc(getSkillId()), 100, skill_lv, skill_get_time(getSkillId(), skill_lv)));
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_attack(BF_MAGIC, src, src, target, getSkillId(), skill_lv, tick, flag);
    }
}
