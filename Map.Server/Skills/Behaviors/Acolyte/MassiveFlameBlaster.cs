using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// IQ_MASSIVE_F_BLASTER — Inquisitor Massive Flame Blaster. Manual
/// port of <c>rathena-fork/src/map/skills/acolyte/massiveflameblaster.cpp</c>.
///
/// <para>Cast applies the marker SC, broadcasts the cast frame, then
/// runs the splash damage pipeline.</para>
///
/// <para>Ratio: <c>-100 + 2300*lv + 15*POW</c> + <c>150*lv</c> vs
/// Brute or Demon races.</para>
/// </summary>
public sealed class MassiveFlameBlaster : RecursiveDamageSplashSkillImpl
{
    public MassiveFlameBlaster() : base(SkillIds.IQ_MASSIVE_F_BLASTER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: apply skill_get_sc(getSkillId()) at 100% then broadcast + splash.
        // The SC for IQ_MASSIVE_F_BLASTER is the "next attack burns"
        // marker — referenced as the same enum name; missing on our
        // StatusType so omitted. The damage portion still resolves.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Dispatch to damage path (rAthena: skill_castend_damage_id).
        CastendDamageId(src, target, skillLevel, ctx);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + 2300*lv + 15*POW
        var ratio = baseRatio + (-100 + 2300 * skillLevel) + 15 * src.Stats.Pow;
        // +150*lv vs Brute / Demon.
        if (target.Stats.Race == BattleRace.Brute || target.Stats.Race == BattleRace.Demon)
            ratio += 150 * skillLevel;
        return ratio;
    }
}
