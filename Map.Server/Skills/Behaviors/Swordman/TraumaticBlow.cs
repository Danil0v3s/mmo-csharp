using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LK_HEADCRUSH — Lord Knight Head Crush / Traumatic Blow. Manual
/// port of <c>rathena-fork/src/map/skills/swordman/traumaticblow.cpp</c>.
/// Ratio <c>+40*lv</c>. Refuses to land on Boss-class targets.
/// 50% chance to bleed except Demon race / Undead element.
/// </summary>
public sealed class TraumaticBlow : WeaponSkillImpl
{
    private readonly Random _rng;

    public TraumaticBlow() : base(SkillIds.LK_HEADCRUSH) => _rng = Random.Shared;

    public TraumaticBlow(Random? rng = null) : base(SkillIds.LK_HEADCRUSH)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 40 * skillLevel;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if ((target.Stats.Mode & MobMode.Mvp) != 0)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        base.CastendDamageId(src, target, skillLevel, ctx);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var isUndead = target.Stats.DefenseElement == BattleElement.Undead;
        if (isUndead || target.Stats.Race == BattleRace.Demon) return;
        if (_rng.Next(100) < 50)
            ctx.Sc?.Start(target, StatusType.Bleeding, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 30_000, src);
    }
}
