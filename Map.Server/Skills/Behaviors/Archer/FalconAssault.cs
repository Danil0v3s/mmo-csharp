using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// SN_FALCONASSAULT — Sniper Falcon Assault. Manual port of
/// <c>rathena-fork/src/map/skills/archer/falconassault.cpp</c>.
/// Plain misc-attack on the named target.
/// </summary>
public sealed class FalconAssault : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public FalconAssault() : base(SkillIds.SN_FALCONASSAULT) { }

    public FalconAssault(ISkillAttackService? skillAttack = null) : base(SkillIds.SN_FALCONASSAULT)
    {
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Misc, src, src, target, SkillId, skillLevel);
    }
}
