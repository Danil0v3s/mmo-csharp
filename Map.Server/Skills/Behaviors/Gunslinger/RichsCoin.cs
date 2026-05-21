using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_RICHS_COIN — Rebellion Rich's Coin. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/richscoin.cpp</c>.
/// Grants 10 coin orbs to the caster. Coin orb system is TODO.
/// </summary>
public sealed class RichsCoin : SkillImpl
{
    public RichsCoin() : base(SkillIds.RL_RICHS_COIN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
