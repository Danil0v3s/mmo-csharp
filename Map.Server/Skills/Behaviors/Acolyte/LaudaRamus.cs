using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_LAUDARAMUS — Arch Bishop Lauda Ramus. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/laudaramus.cpp</c>.
///
/// <para>Twin to <see cref="LaudaAgnus"/>: cures the
/// "sleep / lock-out" debuff family at <c>(60 + 10 * lv) %</c>
/// chance, or applies +CRIT buff otherwise.</para>
///
/// <para>Cure set: Sleep, Stun, Mandragora, Silence, DeepSleep.</para>
/// </summary>
public sealed class LaudaRamus : SkillImpl
{
    private readonly Random _rng;

    public LaudaRamus() : base(SkillIds.AB_LAUDARAMUS) => _rng = Random.Shared;

    public LaudaRamus(Random? rng = null) : base(SkillIds.AB_LAUDARAMUS) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var sc = ctx.Sc;
        var hasCurable = sc != null && (
            sc.Get(target, StatusType.Sleep) != null ||
            sc.Get(target, StatusType.Stun) != null ||
            sc.Get(target, StatusType.Mandragora) != null ||
            sc.Get(target, StatusType.Silence) != null ||
            sc.Get(target, StatusType.Deepsleep) != null);

        if (hasCurable)
        {
            var chance = 60 + 10 * skillLevel;
            if (_rng.Next(100) >= chance) return;

            sc!.End(target, StatusType.Sleep);
            sc.End(target, StatusType.Stun);
            sc.End(target, StatusType.Mandragora);
            sc.End(target, StatusType.Silence);
            sc.End(target, StatusType.Deepsleep);
            return;
        }

        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Laudaramus,
            val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);

        // TODO: party_foreachsamemap iteration when caster is partied.
    }
}
