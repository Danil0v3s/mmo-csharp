using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Minstrel;

/// <summary>
/// WM_REVERBERATION — Minstrel / Wanderer Reverberation. Mirrors
/// <c>rathena-fork/src/map/skills/minstrel/reverberation.cpp</c>.
///
/// Places a ground unit; when triggered (any enemy approaches),
/// it splashes physical + magical damage in radius 2. Per-trigger
/// damage (400 + 200*lv)% ATK + (400 + 200*lv)% MATK.
///
/// First-port: treats the cast as an immediate splash on the
/// target cell (real "trap" timer ports when SkillUnitService
/// gains trigger support for player-placed traps).
/// </summary>
public sealed class Reverberation : RecursiveDamageSplashSkillImpl
{
    public Reverberation() : base(SkillIds.WM_REVERBERATION) { }

    public override short GetSplashSearchSize(Entity src, ushort skillLevel) => 2;

    public override long SplashDamage(Entity src, Entity victim, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var swing = ctx.Battle.CalcWeaponAttack(src, victim);
        var matk = (src.Stats.MatkMin + src.Stats.MatkMax) / 2;
        var rate = 400 + 200 * skillLevel;
        return swing.Total * rate / 100 + matk * rate / 100;
    }
}
