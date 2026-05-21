using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BA_FROSTJOKER — Bard Unbarring Octave (Frost Joker). Manual port
/// of <c>rathena-fork/src/map/skills/archer/unbarringoctave.cpp</c>.
/// After 3 s delay, victims roll <c>(150 + 50*lv) / 10 %</c> SC_FREEZE.
/// </summary>
public sealed class UnbarringOctave : SkillImpl
{
    private readonly ISkillTimerService? _timers;
    private readonly Random _rng;

    public UnbarringOctave() : base(SkillIds.BA_FROSTJOKER) => _rng = Random.Shared;

    public UnbarringOctave(ISkillTimerService? timers = null, Random? rng = null) : base(SkillIds.BA_FROSTJOKER)
    {
        _timers = timers;
        _rng = rng ?? Random.Shared;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        _timers?.Schedule(src, target, 3000, SkillId, skillLevel, (s, t, lv) => { /* deferred FX */ });
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = (150 + 50 * skillLevel) / 10;
        if (_rng.Next(100) < rate)
            ctx.Sc?.Start(target, StatusType.Freeze, val1: skillLevel, 0, 0, 0, durationMs: 8000, src);
    }
}
