using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_EPICLESIS — Arch Bishop Epiclesis. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/epiclesis.cpp</c>.
///
/// <para>Drops a ground-target Sanctuary-like SkillUnit that also
/// auto-resurrects allies inside its splash on placement (rAthena
/// fires <c>ALL_RESURRECTION</c> on every BL_CHAR in the splash
/// once the unit anchors). The per-tick HP/SP regen pulse + buff
/// application lives on the SkillUnit handler.</para>
/// </summary>
public sealed class Epiclesis : SkillImpl
{
    private readonly Map.Server.Skills.ISkillUnitService? _units;

    public Epiclesis() : base(SkillIds.AB_EPICLESIS) { }

    public Epiclesis(Map.Server.Skills.ISkillUnitService? units = null) : base(SkillIds.AB_EPICLESIS)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skill_unitsetting + map_foreachinallarea(... ALL_RESURRECTION, 1, ...)
        // Place the ground unit first; the auto-resurrect splash needs
        // an existing IPartyMapService.ForEachInArea iterator and a way
        // to invoke ALL_RESURRECTION on each ally in range — deferred
        // until the party/iteration layer lands.
        _units?.Place(src, SkillId, skillLevel, x, y);

        // TODO: on placement, iterate skill_get_splash and cast
        // ALL_RESURRECTION on each BCT_NOENEMY char.
    }
}
