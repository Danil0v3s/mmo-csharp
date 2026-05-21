using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// HVAN_EXPLOSION — Vanilmirth Bio Explosion. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_bioexplosion.cpp</c>.
/// Splashes the explosion + drops intimacy to Hate With Passion +
/// kills the homunculus after a delay. Intimacy / death pipeline +
/// timer are TODO.
/// </summary>
public sealed class BioExplosion : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public BioExplosion() : base(SkillIds.HVAN_EXPLOSION) { }

    public BioExplosion(ISkillAttackService? skillAttack = null) : base(SkillIds.HVAN_EXPLOSION)
    {
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src == target) return;
        _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
}
