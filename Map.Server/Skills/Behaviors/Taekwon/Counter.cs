using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// TK_COUNTER — Counter Kick. Manual port of
/// <c>rathena-fork/src/map/skills/taekwon/counter.cpp</c>.
/// +90 + 30*lv ratio.
/// </summary>
public sealed class Counter : WeaponSkillImpl
{
    public Counter() : base(SkillIds.TK_COUNTER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 90 + 30 * skillLevel;
}
