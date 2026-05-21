using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_ACIDTERROR — Alchemist Acid Terror. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/acidterror.cpp</c>.
/// Renewal ratio: <c>+(-100 + 200*lv)</c>. On-hit SC_BLEEDING at
/// <c>3*lv %</c> and a chance to break armor (break_equip TODO).
/// </summary>
public sealed class AcidTerror : WeaponSkillImpl
{
    private readonly Random _rng;

    public AcidTerror() : base(SkillIds.AM_ACIDTERROR) => _rng = Random.Shared;

    public AcidTerror(Random? rng = null) : base(SkillIds.AM_ACIDTERROR) => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 200 * skillLevel);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 3 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Bleeding, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 30_000, src);
        // TODO: skill_break_equip(EQP_ARMOR, ...) + ET_HUK emote.
    }
}
