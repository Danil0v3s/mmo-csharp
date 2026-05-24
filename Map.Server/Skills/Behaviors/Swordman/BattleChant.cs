using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// PA_GOSPEL — Paladin Gospel / Battle Chant. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/battlechant.cpp</c>.
/// If the caster is already running their own Gospel (val4 == BCT_SELF)
/// the field is dispelled and consumables refunded. Otherwise drops the
/// unit group and applies SC_GOSPEL with val4 = BCT_SELF.
/// </summary>
public sealed class BattleChant : SkillImpl
{
    private const int BctSelf = 0x10; // rAthena BCT_SELF
    private readonly ISkillUnitService? _units;

    public BattleChant() : base(SkillIds.PA_GOSPEL) { }

    public BattleChant(ISkillUnitService? units = null) : base(SkillIds.PA_GOSPEL)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var existing = ctx.Sc?.Get(src, StatusType.Gospel);
        if (existing != null && existing.Val4 == BctSelf)
        {
            ctx.Sc?.End(src, StatusType.Gospel);
            // Deferred: SKILL_NOCONSUME_REQ requirement refund — needs the
            // skill-cost / requirement pipeline to expose a refund hook.
            return;
        }
        _units?.Place(src, SkillId, skillLevel, src.X, src.Y);
        if (existing != null)
            ctx.Sc?.End(src, StatusType.Gospel);
        ctx.Sc?.Start(src, StatusType.Gospel, val1: skillLevel, 0, 0, BctSelf, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
