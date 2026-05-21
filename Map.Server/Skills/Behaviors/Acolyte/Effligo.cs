using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CD_EFFLIGO — Cardinal Effligo. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/effligo.cpp</c>.
///
/// <para>Mace melee hit. Emits the cast-frame broadcast before the
/// weapon hit lands. Ratio: <c>-100 + 1650*lv + 7*POW</c> +150*lv
/// vs Undead/Demon. CD_MACE_BOOK_M mastery omitted.</para>
/// </summary>
public sealed class Effligo : WeaponSkillImpl
{
    public Effligo() : base(SkillIds.CD_EFFLIGO) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 1650 * skillLevel) + 7 * src.Stats.Pow;
        if (target.Stats.Race == BattleRace.Undead || target.Stats.Race == BattleRace.Demon)
            ratio += 150 * skillLevel;
        return ratio;
    }
}
