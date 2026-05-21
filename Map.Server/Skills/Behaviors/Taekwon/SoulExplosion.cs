using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SP_SOULEXPLOSION — Soul Explosion. Magic-type detonation.</summary>
public sealed class SoulExplosion : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public SoulExplosion() : base(SkillIds.SP_SOULEXPLOSION) { }
    public SoulExplosion(ISkillAttackService? skillAttack = null) : base(SkillIds.SP_SOULEXPLOSION) { _skillAttack = skillAttack; }
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
