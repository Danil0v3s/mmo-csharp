using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_GRENADE_FRAGMENT — Night Watch Grenade Fragment endow. Manual
/// port of <c>rathena-fork/src/map/skills/gunslinger/grenadefragment.cpp</c>.
/// Lv 1-6 endow the caster with the corresponding elemental SC; lv 7
/// dispels them all. Per-element SC enums + dispel logic are TODO.
/// </summary>
public sealed class GrenadeFragment : SkillImpl
{
    public GrenadeFragment() : base(SkillIds.NW_GRENADE_FRAGMENT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
}
