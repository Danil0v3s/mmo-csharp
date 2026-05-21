using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// CR_GRANDCROSS — Crusader Grand Cross. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/grandcross.cpp</c>.
/// Drops the cross-shaped ground unit at the cast cell. Splash blind
/// proc vs Undead/Demon (not players) is wired through ApplyAdditionalEffects.
/// </summary>
public sealed class GrandCross : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public GrandCross() : base(SkillIds.CR_GRANDCROSS) { }

    public GrandCross(ISkillUnitService? units = null) : base(SkillIds.CR_GRANDCROSS)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is PlayerEntity) return;
        // 100% chance to blind on Demon race or Undead element.
        var isUndead = target.Stats.DefenseElement == BattleElement.Undead;
        if (isUndead || target.Stats.Race == BattleRace.Demon)
            ctx.Sc?.Start(target, StatusType.Blind, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
