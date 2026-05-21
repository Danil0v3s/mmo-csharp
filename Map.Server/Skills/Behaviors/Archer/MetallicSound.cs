using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_METALICSOUND — Minstrel/Wanderer Metallic Sound. Manual port of
/// <c>rathena-fork/src/map/skills/archer/metallicsound.cpp</c>.
///
/// <para>Magic-attack against sleeping targets. Ratio:
/// <c>+(-100 + 120*lv) + 60*WM_LESSON</c> (passive lookup TODO); +100
/// when target carries SC_SLEEP; ×1.5 when target carries
/// SC_SOUNDBLEND.</para>
/// </summary>
public sealed class MetallicSound : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public MetallicSound() : base(SkillIds.WM_METALICSOUND) { }

    public MetallicSound(ISkillAttackService? skillAttack = null) : base(SkillIds.WM_METALICSOUND)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 120 * skillLevel) + 60;
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
