using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_SHINKIROU — Mirage. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/mirage.cpp</c>.
/// Self-buff + cell unit placement at target cell.
/// </summary>
public sealed class Mirage : SkillImpl
{
    // skill_db.yml Duration1 (20000 ms).
    private const int ShinkirouDurationMs = 20_000;

    private readonly ISkillUnitService? _units;

    public Mirage() : base(SkillIds.SS_SHINKIROU) { }

    public Mirage(ISkillUnitService? units = null) : base(SkillIds.SS_SHINKIROU)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        // rAthena: sc_start(src, src, skill_get_sc(SS_SHINKIROU), 100, lv, time).
        // skill_db.yml maps this skill's Status to Shinkirou_Call → SC_SHINKIROU_CALL.
        ctx.Sc?.Start(src, StatusType.ShinkirouCall, val1: skillLevel, 0, 0, 0, durationMs: ShinkirouDurationMs, src);
        _units?.Place(src, SkillId, skillLevel, x, y);
    }
}
