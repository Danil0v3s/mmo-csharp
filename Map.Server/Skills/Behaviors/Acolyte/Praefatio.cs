using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_PRAEFATIO — Arch Bishop Praefatio. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/praefatio.cpp</c>.
///
/// <para>Party-wide Kyrie-style damage-absorb shield. rAthena
/// dispatches via <c>sc_start4(SC_KYRIE, lv, 0, 0, party_count,
/// skill_get_time)</c> — the skill_db row shows <c>Status: Kyrie</c>,
/// confirming the SC reuse. Val4 carries the party-member count so
/// the shield HP scales with team size.</para>
///
/// <para>With a party, the cast walks <c>party_foreachsamemap</c> and
/// re-enters CastendNoDamageId on every party member within splash.
/// Solo (or non-PC) casters apply the SC directly to the target.</para>
/// </summary>
public sealed class Praefatio : SkillImpl
{
    public Praefatio() : base(SkillIds.AB_PRAEFATIO) { }

    /// <summary>rAthena skill_db Duration1 for AB_PRAEFATIO — 120s.</summary>
    private const int PraefatioDurationMs = 120_000;

    /// <summary>Splash radius the party-loop targets.</summary>
    private const short PartySplash = 7;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena's flow:
        //   if !sd || party_id == 0 || flag & 1 -> apply SC + emit
        //   else                                 -> party_foreachsamemap fan-out
        // We resolve party count once and apply to every matching ally so
        // the per-member SC carries Val4 = party member count.
        var partyCount = 1;
        if (src is PlayerEntity pc && pc.PartyId > 0 && ctx.PartyMap != null)
        {
            // Count first so Val4 reflects the full ally count even on the
            // self-application below.
            partyCount = 0;
            ctx.PartyMap.ForEachOnSameMap(pc, _ => partyCount++);
            if (partyCount == 0) partyCount = 1;
        }

        // Always apply to the resolved target (solo path or re-entry tip).
        ApplyKyrie(src, target, skillLevel, partyCount, ctx);

        // Party fan-out — walk all party members on the same map within
        // splash and apply the same SC.
        if (src is PlayerEntity caster && caster.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(caster, member =>
            {
                if (member.Id == target.Id) return;
                var dx = Math.Abs(member.X - src.X);
                var dy = Math.Abs(member.Y - src.Y);
                if (Math.Max(dx, dy) > PartySplash) return;
                ApplyKyrie(src, member, skillLevel, partyCount, ctx);
            });
        }
    }

    private void ApplyKyrie(Entity src, Entity target, ushort skillLevel, int partyCount, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // rAthena: sc_start4(SC_KYRIE, 100, lv, 0, 0, party_count, time).
        // The Kyrie SC handler scales shield HP from Val1 (level) + Val4
        // (party multiplier).
        ctx.Sc?.Start(target, StatusType.Kyrie,
            val1: skillLevel, val2: 0, val3: 0, val4: partyCount,
            durationMs: PraefatioDurationMs, src);
    }
}
