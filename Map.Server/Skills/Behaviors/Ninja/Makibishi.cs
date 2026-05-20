using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_MAKIBISHI — auto-generated stub from
/// <c>src/map/skills/ninja/makibishi.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Makibishi : SkillImpl
{
    public Makibishi() : base(SkillIds.KO_MAKIBISHI) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src,target, SC_STUN, 10 * skill_lv, skill_lv, skill_get_time2(getSkillId(),skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // base_skillratio += -100 + 20 * skill_lv;
    return baseRatio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // for( int32 i = 0; i < (skill_lv+2); i++ ) {
    // 		x = src->x - 1 + rnd()%3;
    // 		y = src->y - 1 + rnd()%3;
    // 		skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    // 	}
    }
}
