using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BD_SIEGFRIED — Bard Acoustic Rhythm (Mr. Kim a Rich Man, retitled
/// Siegfried in renewal). Manual port of
/// <c>rathena-fork/src/map/skills/archer/acousticrhythm.cpp</c>.
///
/// <para>Renewal: a Performer "song" handled by the song dispatcher
/// (party-wide buff per tick). Pre-renewal: drops a ground unit at
/// the cast XY. <c>skill_castend_song</c> isn't wired here yet — we
/// drop the unit (renewal path) and leave the song dispatcher
/// deferred until skill_castend_song lands.</para>
/// </summary>
public sealed class AcousticRhythm : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public AcousticRhythm() : base(SkillIds.BD_SIEGFRIED) { }

    public AcousticRhythm(ISkillUnitService? units = null) : base(SkillIds.BD_SIEGFRIED)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
