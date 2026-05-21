using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SJ_NOVAEXPLOSING — Nova Explosion. Misc-type self-detonation; applies SC_NOVAEXPLOSING to caster.</summary>
public sealed class NovaExplosion : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public NovaExplosion() : base(SkillIds.SJ_NOVAEXPLOSING) { }
    public NovaExplosion(ISkillAttackService? skillAttack = null) : base(SkillIds.SJ_NOVAEXPLOSING) { _skillAttack = skillAttack; }
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Misc, src, src, target, SkillId, skillLevel);
}
