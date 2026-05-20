using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BA_DISSONANCE — auto-generated stub from
/// <c>src/map/skills/archer/unchainedserenade.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class UnchainedSerenade : WeaponSkillImpl
{
    public UnchainedSerenade() : base(SkillIds.BA_DISSONANCE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	const map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	base_skillratio += 10 + skill_lv * 50;
    // 	if (sd != nullptr)
    // 		base_skillratio = base_skillratio * sd->status.job_level / 10;
    // #endif
    return baseRatio;
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
