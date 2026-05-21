using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SJ_FLASHKICK — Flash Kick. Manual port of
/// <c>rathena-fork/src/map/skills/taekwon/flashkick.cpp</c>.
/// Tags the target with SC_FLASHKICK + stellar-mark slot. Tag list
/// management (sd-&gt;stellar_mark[]) is TODO — animation + hit only.
/// </summary>
public sealed class FlashKick : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public FlashKick() : base(SkillIds.SJ_FLASHKICK) { }

    public FlashKick(ISkillAttackService? skillAttack = null) : base(SkillIds.SJ_FLASHKICK)
    {
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
}
