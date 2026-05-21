using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_STONE_RAIN — Elemental Stone Rain. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/stonerain.cpp</c>.
/// +200 ratio; 30% splash, else single hit. Splash branch not yet
/// implemented (no skill_area_sub plumbing in elementalNPC layer).
/// </summary>
public sealed class StoneRain : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public StoneRain() : base(SkillIds.EL_STONE_RAIN) { }

    public StoneRain(ISkillAttackService? skillAttack = null) : base(SkillIds.EL_STONE_RAIN)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 200;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        // TODO: 30% splash via skill_area_sub — for now single hit only.
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }
}
