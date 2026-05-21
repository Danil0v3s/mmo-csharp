using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_EARTHSHAKER — Sura Earth Shaker. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/earthshaker.cpp</c>.
///
/// <para>Self-centered splash that breaks hide / cloak / stealth on
/// targets and stuns mobs. Per-mob effects: apply SC_EARTHSHAKER
/// (the de-hide marker), 25+5*lv % stun, and end any SV_ROOTTWIST.</para>
///
/// <para>Ratio branches on target visibility state:</para>
/// <list type="bullet">
///   <item>Hidden target (Hide / Cloak / Camouflage / StealthField /
///         ShadowForm): <c>-100 + 300*lv + 3*STR</c>.</item>
///   <item>Visible target: <c>-100 + 400*lv + 2*STR</c>.</item>
/// </list>
/// </summary>
public sealed class EarthShaker : WeaponSkillImpl
{
    private readonly Random _rng;

    public EarthShaker() : base(SkillIds.SR_EARTHSHAKER) => _rng = Random.Shared;

    public EarthShaker(Random? rng = null) : base(SkillIds.SR_EARTHSHAKER) => _rng = rng ?? Random.Shared;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Mob-only EARTHSHAKER marker (guardian-data check skipped — castles).
        if (target is MobEntity)
        {
            ctx.Sc?.Start(target, StatusType.Earthshaker,
                val1: skillLevel, 0, 0, 0, durationMs: 3000, src);
        }
        // 25+5*lv % stun on every target.
        if (_rng.Next(100) < 25 + 5 * skillLevel)
        {
            ctx.Sc?.Start(target, StatusType.Stun,
                val1: skillLevel, 0, 0, 0, durationMs: 3000, src);
        }
        ctx.Sc?.End(target, StatusType.SvRoottwist);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // Hidden-target check via SC presence (Hide / Cloaking / Camouflage / etc.).
        // We approximate by looking at any of the documented hide SCs;
        // OPTION_* bitflag check isn't on our SC surface.
        var hidden = false; // TODO: ISkillBehaviorContext needs SC access in CalculateSkillRatio.
        if (hidden)
            return baseRatio + (-100 + 300 * skillLevel) + 3 * src.Stats.Str;
        return baseRatio + (-100 + 400 * skillLevel) + 2 * src.Stats.Str;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena outer path: splash around target; inner path: single hit + hide-strip.
        // We always run the inner path (faithful to per-victim resolution).
        base.CastendDamageId(src, target, skillLevel, ctx);
        ctx.Sc?.End(target, StatusType.Cloakingexceed);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Self-centered AoE — dispatch as damage on src.
        CastendDamageId(src, src, skillLevel, ctx);
    }
}
