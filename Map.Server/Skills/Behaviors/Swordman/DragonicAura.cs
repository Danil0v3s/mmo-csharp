using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// DK_DRAGONIC_AURA — Dragon Knight Dragonic Aura. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/dragonicaura.cpp</c>.
/// Ratio <c>+3650*lv + 10*POW</c>; <c>+150*lv</c> against Demi-Human / Angel.
/// On cast applies SC_DRAGONIC_AURA to the caster.
/// </summary>
public sealed class DragonicAura : WeaponSkillImpl
{
    public DragonicAura() : base(SkillIds.DK_DRAGONIC_AURA) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + 3650 * skillLevel + 10 * src.Stats.Pow;
        if (target.Stats.Race == BattleRace.Demihuman || target.Stats.Race == BattleRace.Angel)
            ratio += 150 * skillLevel;
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
        ctx.Sc?.Start(src, StatusType.DragonicAura, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
