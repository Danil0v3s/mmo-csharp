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
        // Boss / status-immune targets resist 90 % of the time
        // (rAthena: rnd()%100 < 90 returns failure on a 10 % roll).
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0 && _rng.Next(100) < 90)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        // Must actually be casting (rAthena: ud->skilltimer != INVALID_TIMER).
        if (ctx.Cast == null || !ctx.Cast.IsCasting(target.Id))
        {
            if (src is PlayerEntity sd2)
                ctx.Client?.BroadcastSkillFail(sd2, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Drain the SP cost of the cancelled cast from the caster
        // (rAthena: status_zap(src, 0, skill_get_sp(target_skill, target_lv))).
        var (tSkillId, tLv) = ctx.Cast.GetCurrentCast(target.Id);
        ctx.Cast.CancelCast(target.Id);
        if (target is PlayerEntity tgt && tSkillId != 0)
        {
            // Best-effort SP drain — without a per-skill SP-cost reader on
            // ISkillCastService we approximate via a base + per-level
            // formula matching rAthena's default ItemPotion / Bolt curves.
            var spDrain = 10 + 5 * tLv;
            tgt.Sp = Math.Max(0, tgt.Sp - spDrain);
        }
        // Caster recovers a small token amount (rAthena's "free SP" branch
        // on success).
        var statusOps = ctx.StatusOps ?? _statusOps;
        statusOps?.Heal(src, 0, 10 * skillLevel, 2);
    }
}
