using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// SM_PROVOKE — Swordsman Provoke. Mirrors
/// <c>rathena-fork/src/map/skills/swordman/provoke.cpp</c>.
///
/// Applies <see cref="StatusType.Provoke"/> on the target:
///   −5 %/lv DEF, +2 %/lv ATK (renewal). Duration <c>30 − lv</c>s.
///   Undead element + Boss-mode targets are immune.
/// </summary>
public sealed class Provoke : SkillImpl
{
    public Provoke() : base(SkillIds.SM_PROVOKE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return;
        if (target.Stats.DefenseElement == BattleElement.Undead) return;
        if ((target.Stats.Mode & MobMode.Mvp) != 0) return;

        var durationMs = 30_000 - 1_000 * skillLevel;
        ctx.Sc.Start(target, StatusType.Provoke, val1: skillLevel, 0, 0, 0, durationMs, src);
    }
}
