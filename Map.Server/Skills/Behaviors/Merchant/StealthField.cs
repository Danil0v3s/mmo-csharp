using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_STEALTHFIELD — Mechanic Stealth Field. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/stealthfield.cpp</c>.
/// If the caster already owns SC_STEALTHFIELD_MASTER the field is
/// dispelled and consumables are refunded; otherwise drops the unit
/// group at the caster's current cell and starts SC_STEALTHFIELD_MASTER.
/// </summary>
public sealed class StealthField : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public StealthField() : base(SkillIds.NC_STEALTHFIELD) { }

    public StealthField(ISkillUnitService? units = null) : base(SkillIds.NC_STEALTHFIELD)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc != null && ctx.Sc.Get(src, StatusType.StealthfieldMaster) != null)
        {
            ctx.Sc.End(src, StatusType.StealthfieldMaster);
            // Deferred: rAthena sets `flag |= SKILL_NOCONSUME_REQ` so the cast-end
            // consume pass refunds the magic-gear-fuel. The skill-flag plumbing
            // back to SkillCastService.ResolveSkill isn't exposed yet.
            return;
        }
        _units?.Place(src, SkillId, skillLevel, src.X, src.Y);
        ctx.Sc?.Start(src, StatusType.StealthfieldMaster, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
