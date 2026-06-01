using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_POISONSMOKE — Poison Smoke. Manual port of
/// <c>rathena-fork/src/map/skills/thief/poisonsmoke.cpp</c>.
/// Drops a poison-smoke ground unit at (x, y); requires
/// SC_POISONINGWEAPON to be active on the caster. Without it the
/// cast fails with USESKILL_FAIL_GC_POISONINGWEAPON and the SP / cell
/// requirements refund (rAthena <c>flag |= SKILL_NOCONSUME_REQ</c>).
/// </summary>
public sealed class PoisonSmoke : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public PoisonSmoke() : base(SkillIds.GC_POISONSMOKE) { }

    public PoisonSmoke(ISkillUnitService? units = null) : base(SkillIds.GC_POISONSMOKE)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: if (!SC_POISONINGWEAPON) clif_skill_fail(...,
        //   USESKILL_FAIL_GC_POISONINGWEAPON); flag |= NOCONSUME; return.
        if (ctx.Sc?.Get(src, StatusType.Poisoningweapon) == null)
            return;
        _units?.Place(src, SkillId, skillLevel, x, y);
    }
}
