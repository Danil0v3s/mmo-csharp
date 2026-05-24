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
        // Visible-target ratio — the hidden-target +300*lv branch lands in
        // CastendDamageId where the SC reader is available.
        return baseRatio + (-100 + 400 * skillLevel) + 2 * src.Stats.Str;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Hidden-target check via SC presence (Hide / Cloaking / Camouflage /
        // StealthField / ShadowForm). When hidden, override the ratio path
        // before invoking the standard weapon pipeline.
        var hidden = ctx.Sc != null && (
            ctx.Sc.Get(target, StatusType.Hiding) != null
            || ctx.Sc.Get(target, StatusType.Cloaking) != null
            || ctx.Sc.Get(target, StatusType.Cloakingexceed) != null
            || ctx.Sc.Get(target, StatusType.Camouflage) != null
            || ctx.Sc.Get(target, StatusType.Stealthfield) != null
            || ctx.Sc.Get(target, StatusType.Shadowform) != null);
        if (hidden)
        {
            var swing = ctx.Battle.CalcWeaponAttack(src, target);
            var ratio = 100 + (-100 + 300 * skillLevel) + 3 * src.Stats.Str;
            var dmg = (int)Math.Clamp((long)swing.Total * ratio / 100, 0, int.MaxValue);
            ctx.Damage.ApplyDamage(target, dmg, src);
            ApplyAdditionalEffects(src, target, skillLevel, ctx);
        }
        else
        {
            base.CastendDamageId(src, target, skillLevel, ctx);
        }
        ctx.Sc?.End(target, StatusType.Cloakingexceed);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Self-centered AoE — dispatch as damage on src.
        CastendDamageId(src, src, skillLevel, ctx);
    }
}
