using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_FIRE_BOMB — Elemental Fire Bomb. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/firebomb.cpp</c>.
/// +400 ratio; 30% splash via EL_FIRE_BOMB_ATK, else direct skill hit.
/// EL_FIRE_BOMB_ATK splash variant lives in the same file (+200).
/// </summary>
public sealed class FireBomb : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public FireBomb() : base(SkillIds.EL_FIRE_BOMB) { }

    public FireBomb(ISkillAttackService? skillAttack = null) : base(SkillIds.EL_FIRE_BOMB)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 400;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        // TODO: EL_FIRE_BOMB_ATK splash isn't yet a registered SkillId — treat
        // as single direct hit; 30% splash branch deferred.
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }
}
