using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_FROSTMISTY — Warlock Frosty Misty. Manual port of
/// <c>rathena-fork/src/map/skills/mage/frostymisty.cpp</c>.
///
/// <para>Wall-piercing AOE Water magic. Ratio:
/// <c>+(-100 + 200 + 100*lv)</c>. Splash victims roll
/// <c>25 + 5*lv %</c> SC_FREEZING (passes through walls) and 100 %
/// SC_MISTY_FROST. Damage hit additionally requires line of sight
/// via <c>ctx.Paths.PathSearchLong</c>; the SCs apply regardless of
/// LoS.</para>
/// </summary>
public sealed class FrostyMisty : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    private readonly Random _rng;

    public FrostyMisty() : base(SkillIds.WL_FROSTMISTY) => _rng = Random.Shared;

    public FrostyMisty(ISkillAttackService? skillAttack = null, Random? rng = null) : base(SkillIds.WL_FROSTMISTY)
    {
        _skillAttack = skillAttack;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 200 + 100 * skillLevel);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // SCs land regardless of LoS.
        if (_rng.Next(100) < 25 + 5 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Freezing, val1: skillLevel, 0, 0, 0, durationMs: 8_000, src);
        ctx.Sc?.Start(target, StatusType.MistyFrost, val1: skillLevel, 0, 0, 0, durationMs: 8_000, src);
        // Damage hit requires LoS (rAthena map_foreachinrange + wall_check).
        if (ctx.Paths != null && !ctx.Paths.PathSearchLong(src.MapId, src.X, src.Y, target.X, target.Y))
            return;
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }
}
