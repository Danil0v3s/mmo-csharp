using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_COLORS_OF_HYUN_ROK — Shaman Colors of Hyun Rok. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/colorsofhyunrok.cpp</c>.
/// Lv 7 dispels all six elemental endows + the Catnip Meteor buff.
/// Lv 1-6 applies the corresponding endow SC.
/// </summary>
public sealed class ColorsofHyunrok : SkillImpl
{
    public ColorsofHyunrok() : base(SkillIds.SH_COLORS_OF_HYUN_ROK) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena SH_COLORS_OF_HYUN_ROK: skill level selects the SC variant.
        StatusType type = skillLevel switch
        {
            1 => StatusType.ColorsOfHyunRok1,
            2 => StatusType.ColorsOfHyunRok2,
            3 => StatusType.ColorsOfHyunRok3,
            4 => StatusType.ColorsOfHyunRok4,
            5 => StatusType.ColorsOfHyunRok5,
            6 => StatusType.ColorsOfHyunRok6,
            7 => StatusType.ColorsOfHyunRokBuff,
            _ => StatusType.None,
        };
        if (type != StatusType.None)
        {
            ctx.Sc?.Start(src, type, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        }
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
