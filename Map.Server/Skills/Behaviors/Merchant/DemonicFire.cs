using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_DEMONIC_FIRE — Genetic Demonic Fire. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/demonicfire.cpp</c>.
/// Drops a fire ground unit. Ratio: <c>+(10 + 20*lv)</c> (Fire
/// Expansion variants — TODO branch on skillLevel > 10).
/// </summary>
public sealed class DemonicFire : RecursiveDamageSplashSkillImpl
{
    private readonly ISkillUnitService? _units;

    public DemonicFire() : base(SkillIds.GN_DEMONIC_FIRE) { }

    public DemonicFire(ISkillUnitService? units = null) : base(SkillIds.GN_DEMONIC_FIRE)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 10 + 20 * skillLevel;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
