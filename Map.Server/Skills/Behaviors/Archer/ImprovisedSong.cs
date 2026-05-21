using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_RANDOMIZESPELL — Minstrel/Wanderer Improvised Song. Manual port
/// of <c>rathena-fork/src/map/skills/archer/improvisedsong.cpp</c>.
/// At <c>30 + 10*lv %</c>, dispels every Performer song SC on the
/// target.
/// </summary>
public sealed class ImprovisedSong : SkillImpl
{
    private static readonly StatusType[] PerformerSongs =
    {
        StatusType.Songofmana,
        StatusType.Dancewithwug,
        StatusType.Leradsdew,
        StatusType.Saturdaynightfever,
        StatusType.Beyondofwarcry,
        StatusType.Melodyofsink,
        StatusType.Unlimitedhummingvoice,
    };

    private readonly Random _rng;

    public ImprovisedSong() : base(SkillIds.WM_RANDOMIZESPELL) => _rng = Random.Shared;

    public ImprovisedSong(Random? rng = null) : base(SkillIds.WM_RANDOMIZESPELL) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) >= 30 + 10 * skillLevel)
            return;
        foreach (var sc in PerformerSongs)
            ctx.Sc?.End(target, sc);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
