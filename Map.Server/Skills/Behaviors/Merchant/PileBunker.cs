using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_PILEBUNKER — Mechanic Pile Bunker. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/pilebunker.cpp</c>.
/// Ratio: <c>+(200 + 100*lv) + STR</c>. On-hit roll
/// <c>25 + 15*lv %</c> ends Kyrie/Assumptio/SteelBody/AutoGuard/etc.
/// </summary>
public sealed class PileBunker : WeaponSkillImpl
{
    private readonly Random _rng;

    public PileBunker() : base(SkillIds.NC_PILEBUNKER) => _rng = Random.Shared;

    public PileBunker(Random? rng = null) : base(SkillIds.NC_PILEBUNKER) => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 200 + 100 * skillLevel + src.Stats.Str;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) >= 25 + 15 * skillLevel) return;
        ctx.Sc?.End(target, StatusType.Kyrie);
        ctx.Sc?.End(target, StatusType.Assumptio);
        ctx.Sc?.End(target, StatusType.Steelbody);
        ctx.Sc?.End(target, StatusType.Autoguard);
        ctx.Sc?.End(target, StatusType.Reflectdamage);
        ctx.Sc?.End(target, StatusType.Defender);
    }
}
