using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// HVAN_CAPRICE — Vanilmirth Caprice. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_caprice.cpp</c>.
/// Randomly picks one of MG_COLDBOLT, MG_FIREBOLT, MG_LIGHTNINGBOLT,
/// WZ_EARTHSPIKE and runs its damage. We dispatch via the magic
/// pipeline using the parent skill id pending sub-skill plumbing.
/// </summary>
public sealed class Caprice : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    private readonly Random _rng;

    public Caprice() : base(SkillIds.HVAN_CAPRICE) => _rng = Random.Shared;

    public Caprice(ISkillAttackService? skillAttack = null, Random? rng = null) : base(SkillIds.HVAN_CAPRICE)
    {
        _skillAttack = skillAttack;
        _rng = rng ?? Random.Shared;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var subskill = _rng.Next(4) switch
        {
            0 => SkillIds.MG_COLDBOLT,
            1 => SkillIds.MG_FIREBOLT,
            2 => SkillIds.MG_LIGHTNINGBOLT,
            _ => SkillIds.WZ_EARTHSPIKE,
        };
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, subskill, skillLevel);
    }
}
