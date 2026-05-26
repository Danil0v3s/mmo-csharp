using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SHC_ETERNAL_SLASH — Eternal Slash. Manual port of
/// <c>rathena-fork/src/map/skills/thief/eternalslash.cpp</c>.
/// Ratio <c>+(-100 + 300*lv) + 2*pow</c>; <c>+120*lv + pow</c> under
/// SC_SHADOW_EXCEED. Each cast bumps the caster's SC_E_SLASH_COUNT
/// (capped at 5) and emits the connect broadcast before the weapon
/// pipeline fires.
///
/// <para>🚩 INFRA-DEFERRED — rAthena reads
/// <c>sc->getSCE(SC_E_SLASH_COUNT)->val1</c> inside
/// <c>modifyDamageData</c> to scale <c>dmg.div_</c>. Our
/// <see cref="SkillImpl.ModifyDamageData"/> hook lacks a
/// <see cref="SkillBehaviorContext"/> param, so the SC-driven hit
/// multiplier is deferred. Tracked: extend ModifyDamageData with a
/// ctx overload.</para>
/// </summary>
public sealed class EternalSlash : WeaponSkillImpl
{
    /// <summary>rAthena <c>min(5, ...)</c> — counter ceiling on
    /// SC_E_SLASH_COUNT.Val1.</summary>
    private const int E_SLASH_MAX = 5;

    public EternalSlash() : base(SkillIds.SHC_ETERNAL_SLASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 300 * skillLevel) + 2 * src.Stats.Pow;
        if (ctx.Sc?.Get(src, StatusType.ShadowExceed) != null)
            ratio += 120 * skillLevel + src.Stats.Pow;
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var existing = ctx.Sc?.Get(src, StatusType.ESlashCount);
        var nextCount = existing != null ? System.Math.Min(E_SLASH_MAX, existing.Val1 + 1) : 1;
        ctx.Sc?.Start(src, StatusType.ESlashCount, val1: nextCount, 0, 0, 0,
            durationMs: 5_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
    }
}
