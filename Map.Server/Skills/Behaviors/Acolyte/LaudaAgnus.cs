using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_LAUDAAGNUS — Arch Bishop Lauda Agnus. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/laudaagnus.cpp</c>.
///
/// <para>Dual-effect cast:</para>
/// <list type="bullet">
///   <item>If the target carries Freeze, Stone, Blind, Burning,
///         Freezing or Crystallize → cure them at
///         <c>(60 + 10 * skillLevel) %</c> chance (no buff applied).</item>
///   <item>Otherwise → apply <see cref="StatusType.Laudaagnus"/> as
///         a +VIT buff for 60 s.</item>
/// </list>
/// </summary>
public sealed class LaudaAgnus : SkillImpl
{
    private readonly Random _rng;

    public LaudaAgnus() : base(SkillIds.AB_LAUDAAGNUS) => _rng = Random.Shared;

    public LaudaAgnus(Random? rng = null) : base(SkillIds.AB_LAUDAAGNUS) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var sc = ctx.Sc;
        var hasCurable = sc != null && (
            sc.Get(target, StatusType.Freeze) != null ||
            sc.Get(target, StatusType.Stone) != null ||
            sc.Get(target, StatusType.Blind) != null ||
            sc.Get(target, StatusType.Burning) != null ||
            sc.Get(target, StatusType.Freezing) != null ||
            sc.Get(target, StatusType.Crystalize) != null);

        if (hasCurable)
        {
            // rAthena: if (rnd()%100 > 60+10*skill_lv) return;
            var chance = 60 + 10 * skillLevel;
            if (_rng.Next(100) >= chance) return;

            sc!.End(target, StatusType.Freeze);
            sc.End(target, StatusType.Stone);
            sc.End(target, StatusType.Blind);
            sc.End(target, StatusType.Burning);
            sc.End(target, StatusType.Freezing);
            sc.End(target, StatusType.Crystalize);
            return;
        }

        // No curable SC — apply the VIT buff.
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Laudaagnus,
            val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);

        // rAthena party_foreachsamemap fan-out: when caster is partied
        // the SC propagates to every same-map partymate.
        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.Laudaagnus, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
            }, includeSelf: false);
        }
    }
}
