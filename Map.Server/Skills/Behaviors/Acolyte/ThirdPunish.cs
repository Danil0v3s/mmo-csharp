using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// IQ_THIRD_PUNISH — Inquisitor Third Punish. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/thirdpunish.cpp</c>.
///
/// <para>Holy splash that consumes the target's
/// <see cref="StatusType.SecondBrand"/> (the prerequisite combo
/// state). Also refills the caster's Spirit Spheres to cap
/// (5 + Raising Dragon Val1) on splash entry.</para>
///
/// <para>Ratio: <c>-100 + 450 + 1800*lv + 10*POW</c>.</para>
/// </summary>
public sealed class ThirdPunish : RecursiveDamageSplashSkillImpl
{
    private readonly IPlayerOrbService? _orbs;

    public ThirdPunish() : base(SkillIds.IQ_THIRD_PUNISH) { }

    public ThirdPunish(IPlayerOrbService? orbs = null) : base(SkillIds.IQ_THIRD_PUNISH)
    {
        _orbs = orbs;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 450 + 1800 * skillLevel) + 10 * src.Stats.Pow;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: status_change_end(target, SC_SECOND_BRAND);
        ctx.Sc?.End(target, StatusType.SecondBrand);

        // rAthena splashSearch hook adds 5 (+ raising-dragon Val1) spirit
        // spheres to the caster. We merge that into the per-victim hook
        // since splashSearch isn't broken out on our base.
        if (src is PlayerEntity sd)
        {
            int limit = 5;
            var raising = ctx.Sc?.Get(sd, StatusType.Raisingdragon);
            if (raising != null) limit += raising.Val1;
            _orbs?.Add(sd, OrbKind.Spirit, limit);
        }
    }
}
