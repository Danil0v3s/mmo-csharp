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
    private const short SplashRadius = 2;
    private readonly Random _rng;
    private readonly ISkillAttackService? _skillAttack;

    public HurricaneRage() : base(SkillIds.EL_HURRICANE) => _rng = Random.Shared;

    public HurricaneRage(ISkillAttackService? skillAttack = null, Random? rng = null) : base(SkillIds.EL_HURRICANE)
    {
        _skillAttack = skillAttack;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 600;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
        if (_rng.Next(100) >= 30) return;
        var victims = ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y, SplashRadius,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == src.Id || v.Id == target.Id) continue;
            _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, v, SkillIds.EL_HURRICANE_ATK, skillLevel);
        }
    }
}
