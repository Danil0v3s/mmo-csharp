using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_DEMONSTRATION — Alchemist Bomb (Demonstration). Manual port of
/// <c>rathena-fork/src/map/skills/merchant/bomb.cpp</c>.
/// Drops a damage-trap ground unit. Ratio <c>+20*lv</c>. On-hit
/// break_equip(weapon) TODO.
/// </summary>
public sealed class Bomb : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public Bomb() : base(SkillIds.AM_DEMONSTRATION) { }

    public Bomb(ISkillUnitService? units = null) : base(SkillIds.AM_DEMONSTRATION)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 20 * skillLevel;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
