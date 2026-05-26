using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_METALICSOUND — Minstrel/Wanderer Metallic Sound. Manual port of
/// <c>rathena-fork/src/map/skills/archer/metallicsound.cpp</c>.
///
/// <para>Magic attack. Ratio: <c>+(-100 + 120*lv) + 60*WM_LESSON</c>;
/// +100 when target sleeps; running ratio x1.5 when target has
/// SC_SOUNDBLEND. Ends SC_SOUNDBLEND on hit.</para>
/// </summary>
public sealed class MetallicSound : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public MetallicSound() : base(SkillIds.WM_METALICSOUND) { }

    public MetallicSound(ISkillAttackService? skillAttack = null) : base(SkillIds.WM_METALICSOUND)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var lesson = (src is PlayerEntity pc) ? (ctx.PlayerSkill?.CheckSkill(pc, SkillIds.WM_LESSON) ?? 0) : 1;
        var ratio = baseRatio + (-100 + 120 * skillLevel) + 60 * lesson;
        if (ctx.Sc != null && ctx.Sc.Get(target, StatusType.Sleep) != null) ratio += 100;
        if (ctx.Sc != null && ctx.Sc.Get(target, StatusType.Soundblend) != null)
            ratio += ratio * 50 / 100;
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.Soundblend);
    }
}
