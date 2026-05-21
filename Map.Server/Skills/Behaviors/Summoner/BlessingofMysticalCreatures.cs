using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_BLESSING_OF_MYSTICAL_CREATURES — Shaman Blessing of Mystical
/// Creatures. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/blessingofmysticalcreatures.cpp</c>.
/// Heals AP up to 200 and applies the buff SC. AP system + SC enum are
/// TODO; we land the animation.
/// </summary>
public sealed class BlessingofMysticalCreatures : SkillImpl
{
    public BlessingofMysticalCreatures() : base(SkillIds.SH_BLESSING_OF_MYSTICAL_CREATURES) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: status_heal AP up to 200, apply SC_BLESSING_OF_MYSTICAL_CREATURES.
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
