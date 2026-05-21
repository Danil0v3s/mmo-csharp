using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_TYPOON_MIS — Elemental Typhoon Missile. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/typhoonmissile.cpp</c>.
/// +900 ratio; 30% splash, else single hit. Applies SC_SILENCE at
/// 10*lv%. EL_TYPOON_MIS_ATK splash variant lives in the same source
/// file (+1100).
/// </summary>
public sealed class TyphoonMissile : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public TyphoonMissile() : base(SkillIds.EL_TYPOON_MIS) { }

    public TyphoonMissile(ISkillAttackService? skillAttack = null) : base(SkillIds.EL_TYPOON_MIS)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 900;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        // TODO: 30% splash via EL_TYPOON_MIS_ATK isn't yet a registered SkillId.
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 10 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Silence, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
    }
}
