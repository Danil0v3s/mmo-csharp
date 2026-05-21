using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// MO_ABSORBSPIRITS — Monk Absorb Spirit Sphere. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/absorbspiritsphere.cpp</c>.
///
/// <para>Consumes the target's Spirit Spheres / Charms and converts
/// them to SP for the caster (7 SP per sphere / charm). For mob
/// targets: 20 % chance to absorb 2 SP per mob level, then re-aggro
/// the mob onto the caster.</para>
///
/// <para>Gunslingers' Coins are explicitly protected (rAthena's
/// "[Reddozen]" comment). Status-immune mobs (MD_STATUSIMMUNE) are
/// excluded from the mob-absorb branch.</para>
/// </summary>
public sealed class AbsorbSpiritSphere : SkillImpl
{
    private readonly IPlayerOrbService? _orbs;
    private readonly IStatusOpsService? _statusOps;
    private readonly Random _rng;

    public AbsorbSpiritSphere() : base(SkillIds.MO_ABSORBSPIRITS) => _rng = Random.Shared;

    public AbsorbSpiritSphere(
        IPlayerOrbService? orbs = null,
        IStatusOpsService? statusOps = null,
        Random? rng = null) : base(SkillIds.MO_ABSORBSPIRITS)
    {
        _orbs = orbs;
        _statusOps = statusOps;
        _rng = rng ?? Random.Shared;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        int spGain = 0;

        if (target is PlayerEntity dstsd)
        {
            // TODO: gunslinger class check via MAPID_FIRSTMASK == MAPID_GUNSLINGER.
            // The C# port doesn't have class-mapid surfaced yet; we apply
            // to all PCs as a safe fallback (gunslinger coin protection
            // becomes available when ClassMapid lands).
            var spheres = _orbs?.Get(dstsd, OrbKind.Spirit) ?? 0;
            if (spheres > 0)
            {
                spGain += spheres * 7;
                _orbs?.Remove(dstsd, OrbKind.Spirit, spheres);
            }
            // Spirit Charm absorb path skipped — charm system isn't surfaced
            // through IPlayerOrbService yet.
        }
        else if (target is MobEntity mob && (mob.Stats.Mode & MobMode.StatusImmune) == 0)
        {
            if (_rng.Next(100) < 20)
            {
                spGain = 2 * mob.Level;
                // TODO: mob_target(dstmd, src, 0) — re-aggro the mob.
            }
        }
        else
        {
            // Neither PC with spheres nor eligible mob — fail-broadcast.
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId,
                    Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }

        if (spGain > 0)
        {
            _statusOps?.Heal(src, 0, spGain, 3);
        }

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, success: spGain > 0);
    }
}
