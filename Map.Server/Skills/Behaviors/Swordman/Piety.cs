using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_PIETY — Royal Guard Piety. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/piety.cpp</c>.
/// Solo cast applies SC_PIETY to the target; with a party splashes to
/// nearby party members and self. SC_PIETY isn't in StatusType yet —
/// TODO; we land the broadcast for now.
/// </summary>
public sealed class Piety : SkillImpl
{
    public Piety() : base(SkillIds.LG_PIETY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: add SC_PIETY enum and start it here; splash via party_foreachsamemap.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
