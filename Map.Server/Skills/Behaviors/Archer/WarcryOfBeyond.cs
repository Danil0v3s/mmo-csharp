using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_BEYOND_OF_WARCRY — Minstrel/Wanderer Warcry of Beyond. Manual
/// port of <c>rathena-fork/src/map/skills/archer/warcryofbeyond.cpp</c>.
///
/// <para>Splash SC apply at <c>12 + 3*lv + WM_LESSON %</c>. Every
/// nearby PC enemy rolls the SC.</para>
/// </summary>
public sealed class WarcryOfBeyond : SkillImpl
{
    private readonly Random _rng;

    public WarcryOfBeyond() : base(SkillIds.WM_BEYOND_OF_WARCRY) => _rng = Random.Shared;
    public WarcryOfBeyond(Random? rng = null) : base(SkillIds.WM_BEYOND_OF_WARCRY) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var lesson = (src is PlayerEntity pc) ? (ctx.PlayerSkill?.CheckSkill(pc, SkillIds.WM_LESSON) ?? 0) : 0;
        var rate = 12 + 3 * skillLevel + lesson;
        if (_rng.Next(100) >= rate) return;

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        const short splash = 6;
        var victims = ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y, splash, EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id.Value == src.Id.Value) continue;
            ctx.Sc?.Start(v, StatusType.Beyondofwarcry, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        }
    }
}
