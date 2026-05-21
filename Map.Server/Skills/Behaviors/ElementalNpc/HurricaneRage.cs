using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_HURRICANE — Elemental Hurricane Rage. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/hurricanerage.cpp</c>.
/// +600 ratio; 30% splash via EL_HURRICANE_ATK, else direct hit.
/// EL_HURRICANE_ATK splash variant lives in the same source file (+400).
/// </summary>
public sealed class HurricaneRage : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public HurricaneRage() : base(SkillIds.EL_HURRICANE) { }

    public HurricaneRage(ISkillAttackService? skillAttack = null) : base(SkillIds.EL_HURRICANE)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 600;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        // TODO: EL_HURRICANE_ATK splash isn't yet a registered SkillId — treat
        // as single direct hit; 30% splash branch deferred.
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }
}
