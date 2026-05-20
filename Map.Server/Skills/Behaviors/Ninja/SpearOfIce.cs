using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_HYOUSENSOU — auto-generated stub from
/// <c>src/map/skills/ninja/spearofice.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SpearOfIce : SkillImpl
{
    public SpearOfIce() : base(SkillIds.NJ_HYOUSENSOU) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_attack(BF_MAGIC, src, src, target, getSkillId(), skill_lv, tick, flag);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // #ifdef RENEWAL
    // 	const status_change *sc = status_get_sc(src);
    // 
    // 	base_skillratio -= 30;
    // 	if (sc && sc->getSCE(SC_SUITON))
    // 		base_skillratio += 2 * skill_lv;
    // #endif
    // 	if(sd && sd->spiritcharm_type == CHARM_TYPE_WATER && sd->spiritcharm > 0)
    // 		base_skillratio += 20 * sd->spiritcharm;
    return baseRatio;
    }
}
