using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_GUARDIAN_SHIELD — Imperial Guard Guardian Shield. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/guardianshield.cpp</c>.
///
/// <para>Solo / no-party cast: applies SC_GUARDIAN_S (the rAthena
/// status row for IG_GUARDIAN_SHIELD, named <c>Guardian_S</c> in the
/// status yml) to the target. Party cast: walks
/// <c>party_foreachsamemap</c> within the skill's splash and applies
/// the same SC to each member.</para>
/// </summary>
public sealed class GuardianShield : SkillImpl
{
    public GuardianShield() : base(SkillIds.IG_GUARDIAN_SHIELD) { }

    /// <summary>rAthena skill_db Duration1 for IG_GUARDIAN_SHIELD — 30s.</summary>
    private const int ShieldDurationMs = 30_000;

    /// <summary>Splash radius around the caster for the party fan-out.</summary>
    private const short PartySplash = 7;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: solo or no-party cast OR re-entered with flag & 1 (the
        // party loop dispatches to castend_nodamage_id per member). The
        // flag-bit branch isn't exposed in our plugin signature today;
        // we always apply solo to the resolved target plus fan out to
        // party members when the caster has one.
        ApplyShield(src, target, skillLevel, ctx);

        if (src is PlayerEntity pc && pc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pc, member =>
            {
                if (member.Id == target.Id) return; // already covered above
                // Distance gate matches rAthena skill_area_sub Chebyshev range.
                var dx = Math.Abs(member.X - src.X);
                var dy = Math.Abs(member.Y - src.Y);
                if (Math.Max(dx, dy) > PartySplash) return;
                ApplyShield(src, member, skillLevel, ctx);
            });
        }
    }

    private void ApplyShield(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // skill_get_sc(IG_GUARDIAN_SHIELD) — rAthena status_db row
        // "Guardian_S" lookup → SC_GUARDIAN_S → StatusType.GuardianS.
        ctx.Sc?.Start(target, StatusType.GuardianS, val1: skillLevel, 0, 0, 0,
            durationMs: ShieldDurationMs, src);
    }
}
