using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_RICHS_COIN — Rebellion Rich's Coin. Port of
/// <c>rathena-fork/src/map/skills/gunslinger/richscoin.cpp</c>.
///
/// Grants 10 coin orbs (rAthena treats Rebellion coins as spirit-balls
/// with a 10-cap, using <c>pc_addspiritball</c> at the caster).
/// </summary>
public sealed class RichsCoin : SkillImpl
{
    private const int CoinGrant = 10;

    public RichsCoin() : base(SkillIds.RL_RICHS_COIN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is PlayerEntity pc && ctx.Orbs != null)
        {
            ctx.Orbs.Add(pc, OrbKind.Spirit, CoinGrant);
        }
    }
}
