using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_SOUNDBLEND — Trouvere/Troubadour Sound Blend. Manual port of
/// <c>rathena-fork/src/map/skills/archer/soundblend.cpp</c>.
///
/// <para>Magic hit + SC_SOUNDBLEND application. Ratio:
/// <c>+(-100 + 120*lv) + 5*SPL</c>. SC_MYSTIC_SYMPHONY on the caster
/// doubles the running ratio with a further x1.5 vs Fish / Demihuman
/// targets.</para>
/// </summary>
public sealed class SoundBlend : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public SoundBlend() : base(SkillIds.TR_SOUNDBLEND) { }

    public SoundBlend(ISkillAttackService? skillAttack = null) : base(SkillIds.TR_SOUNDBLEND)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 120 * skillLevel) + 5 * src.Stats.Spl;
        if (ctx.Sc != null && ctx.Sc.Get(src, StatusType.MysticSymphony) != null)
        {
            ratio *= 2;
            if (target.Stats.Race == BattleRace.Fish || target.Stats.Race == BattleRace.Demihuman)
                ratio += ratio * 50 / 100;
        }
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Soundblend, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 30_000, src);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        CastendDamageId(src, target, skillLevel, ctx);
    }
}
