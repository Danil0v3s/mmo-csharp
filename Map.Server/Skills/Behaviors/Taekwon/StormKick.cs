using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// TK_STORMKICK — Tornado Kick. Manual port of
/// <c>rathena-fork/src/map/skills/taekwon/stormkick.cpp</c>.
/// +60 + 20*lv ratio; splash via map_foreachinshootrange (TODO).
/// </summary>
public sealed class StormKick : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public StormKick() : base(SkillIds.TK_STORMKICK) { }

    public StormKick(ISkillAttackService? skillAttack = null) : base(SkillIds.TK_STORMKICK)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 60 + 20 * skillLevel;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
    }
}
