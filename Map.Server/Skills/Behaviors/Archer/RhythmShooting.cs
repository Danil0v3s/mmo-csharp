using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_RHYTHMSHOOTING — Trouvere Rhythm Shooting. Manual port of
/// <c>rathena-fork/src/map/skills/archer/rhythmshooting.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 550 + 950*lv)</c>; with TR_STAGE_MANNER
/// learned, +5*CON; SC_SOUNDBLEND on target adds <c>300 + 100*lv +
/// 2*CON</c>; SC_MYSTIC_SYMPHONY on the caster doubles the running
/// ratio with a further x1.5 vs Fish / Demihuman targets.</para>
/// </summary>
public sealed class RhythmShooting : WeaponSkillImpl
{
    public RhythmShooting() : base(SkillIds.TR_RHYTHMSHOOTING) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 550 + 950 * skillLevel);
        var hasStage = (src is PlayerEntity pc) && (ctx.PlayerSkill?.CheckSkill(pc, SkillIds.TR_STAGE_MANNER) ?? 0) > 0;
        if (hasStage) ratio += 5 * src.Stats.Con;

        if (ctx.Sc != null && ctx.Sc.Get(target, StatusType.Soundblend) != null)
        {
            ratio += 300 + 100 * skillLevel;
            ratio += 2 * src.Stats.Con;
        }

        if (ctx.Sc != null && ctx.Sc.Get(src, StatusType.MysticSymphony) != null)
        {
            ratio *= 2;
            if (target.Stats.Race == BattleRace.Fish || target.Stats.Race == BattleRace.Demihuman)
                ratio += ratio * 50 / 100;
        }
        return ratio;
    }
}
