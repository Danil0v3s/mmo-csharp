using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_KAMAITACHI — Kamaitachi. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/kamaitachi.cpp</c>.
/// Directional AoE; ratio +100*lv (charm bonus deferred). Drops a
/// unit at the pos2 cell and lays directional damage. Directional
/// AoE iteration is TODO.
/// </summary>
public sealed class Kamaitachi : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    private readonly ISkillUnitService? _units;

    public Kamaitachi() : base(SkillIds.NJ_KAMAITACHI) { }

    public Kamaitachi(ISkillAttackService? skillAttack = null, ISkillUnitService? units = null) : base(SkillIds.NJ_KAMAITACHI)
    {
        _skillAttack = skillAttack;
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * skillLevel;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
