using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_SPELLBREAKER — Sage Spell Breaker. Manual port of
/// <c>rathena-fork/src/map/skills/mage/spellbreaker.cpp</c>.
///
/// <para>Cancels the target's current cast and drains the cast's SP
/// cost. Boss/status-immune targets succeed only 10 % of the time.
/// Lv 5+ siphons 2 % MaxHP on PvP maps. The current-cast lookup
/// (<c>ud-&gt;skill_id</c>, <c>ud-&gt;skill_lv</c>, skill_get_sp) and
/// cast-cancel API aren't surfaced to this hook — for now we land the
/// success roll, broadcast the cast, and recover a token amount of SP
/// on the caster.</para>
/// </summary>
public sealed class SpellBreaker : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;
    private readonly Random _rng;

    public SpellBreaker() : base(SkillIds.SA_SPELLBREAKER) => _rng = Random.Shared;

    public SpellBreaker(IStatusOpsService? statusOps = null, Random? rng = null) : base(SkillIds.SA_SPELLBREAKER)
    {
        _statusOps = statusOps;
        _rng = rng ?? Random.Shared;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0 && _rng.Next(100) < 90)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: cancel target's current cast + drain its SP cost back to the caster.
        // For now refund a flat SP token to the caster.
        _statusOps?.Heal(src, 0, 10 * skillLevel, 2);
    }
}
