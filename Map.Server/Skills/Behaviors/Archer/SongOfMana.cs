using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_SONG_OF_MANA — Minstrel/Wanderer Song of Mana. Manual port of
/// <c>rathena-fork/src/map/skills/archer/songofmana.cpp</c>.
/// Party-wide SP-regen buff. Splash via party_foreachsamemap TODO.
/// </summary>
public sealed class SongOfMana : SkillImpl
{
    public SongOfMana() : base(SkillIds.WM_SONG_OF_MANA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Songofmana, val1: skillLevel, val2: 5, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
