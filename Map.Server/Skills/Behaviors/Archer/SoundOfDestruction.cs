using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_SOUND_OF_DESTRUCTION — Minstrel/Wanderer Sound of Destruction.
/// Manual port of <c>rathena-fork/src/map/skills/archer/soundofdestruction.cpp</c>.
///
/// <para>Splash debuff. Every nearby PC enemy receives the SC at 100 %;
/// the SC's duration takes a <c>WM_LESSON * 500 ms</c> bonus on top of
/// the per-skill base.</para>
/// </summary>
public sealed class SoundOfDestruction : SkillImpl
{
    public SoundOfDestruction() : base(SkillIds.WM_SOUND_OF_DESTRUCTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var lesson = (src is PlayerEntity pc) ? (ctx.PlayerSkill?.CheckSkill(pc, SkillIds.WM_LESSON) ?? 0) : 0;
        var duration = 30_000 + lesson * 500;

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        const short splash = 7;
        var victims = ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y, splash, EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id.Value == src.Id.Value) continue;
            ctx.Sc?.Start(v, StatusType.Soundofdestruction, val1: skillLevel, 0, 0, 0, durationMs: duration, src);
        }
    }
}
