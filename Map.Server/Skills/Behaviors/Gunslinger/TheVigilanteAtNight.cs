using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_THE_VIGILANTE_AT_NIGHT — Night Watch The Vigilante at Night.
/// Manual port of <c>rathena-fork/src/map/skills/gunslinger/thevigilanteatnight.cpp</c>.
/// Ratio (shotgun) <c>+(-100 + 800 + 700*lv) + 5*CON</c>. Gatling variant
/// uses a different ratio (TODO — needs weapon-type plumbing).
/// </summary>
public sealed class TheVigilanteAtNight : SkillImpl
{
    public TheVigilanteAtNight() : base(SkillIds.NW_THE_VIGILANTE_AT_NIGHT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 800 + 700 * skillLevel) + 5 * src.Stats.Con;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
