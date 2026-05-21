using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_COLORS_OF_HYUN_ROK — Shaman Colors of Hyun Rok. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/colorsofhyunrok.cpp</c>.
/// Lv 7 dispels all six elemental endows + the Catnip Meteor buff.
/// Lv 1-6 applies the corresponding endow SC. SC enums for the
/// per-color variants aren't in StatusType yet — TODO; we land the
/// animation.
/// </summary>
public sealed class ColorsofHyunrok : SkillImpl
{
    public ColorsofHyunrok() : base(SkillIds.SH_COLORS_OF_HYUN_ROK) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: SC_COLORS_OF_HYUN_ROK_1..6 + SC_COLORS_OF_HYUN_ROK_BUFF enums.
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
