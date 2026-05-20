using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// TF_BACKSLIDING — auto-generated stub from
/// <c>src/map/skills/thief/backslide.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class BackSlide : SkillImpl
{
    public BackSlide() : base(SkillIds.TF_BACKSLIDING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // //This is the correct implementation as per packet logging information. [Skotlex]
    // 
    // 	// Backsliding makes you immune to being stopped for 200ms, but only if you don't have the endure effect yet
    // 	if (unit_data *ud = unit_bl2ud(bl); ud != nullptr && !status_isendure(*bl, tick, true))
    // 		ud->endure_tick = tick + 200;
    // 
    // #ifdef RENEWAL
    // 	int16 blew_count = skill_blown(src, bl, skill_get_blewcount(getSkillId(), skill_lv), unit_getdir(bl),
    // 	                               static_cast<enum e_skill_blown>(BLOWN_IGNORE_NO_KNOCKBACK | BLOWN_DONT_SEND_PACKET));
    // 	clif_skill_nodamage(src, *bl, getSkillId(), skill_lv);
    // 
    // 	if (blew_count > 0)
    // 		clif_blown(src); // Always blow, otherwise it shows a casting animation. [Lemongrass]
    // #else
    // 	int16 blew_count = skill_blown(src, bl, skill_get_blewcount(getSkillId(), skill_lv), unit_getdir(bl), BLOWN_IGNORE_NO_KNOCKBACK);
    // 	clif_skill_nodamage(src, *bl, getSkillId(), skill_lv);
    // 	clif_slide(*bl, bl->x, bl->y); //Show the casting animation on pre-re
    // #endif
    }
}
