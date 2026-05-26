using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_REVERBERATION — Minstrel/Wanderer Reverberation. Manual port of
/// <c>rathena-fork/src/map/skills/archer/reverberation.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 700 + 300*lv)</c>; x1.5 when target has
/// SC_SOUNDBLEND. Hit dispatches to the magic-attack pipe via
/// <see cref="ISkillAttackService"/>; SC_SOUNDBLEND is ended after the
/// hit. rAthena also splash-fires the magic hit on every BL_CHAR/BL_SKILL
/// in range — splash uses
/// <see cref="IEntityRegistry.ForEachInRange"/>.</para>
/// </summary>
public sealed class Reverberation : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public Reverberation() : base(SkillIds.WM_REVERBERATION) { }

    public Reverberation(ISkillAttackService? skillAttack = null) : base(SkillIds.WM_REVERBERATION)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 700 + 300 * skillLevel);
        if (ctx.Sc != null && ctx.Sc.Get(target, StatusType.Soundblend) != null)
            ratio += ratio * 50 / 100;
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);

        // Splash fan-out — every nearby BL_CHAR enemy takes the hit.
        const short splash = 2;
        var victims = ctx.Entities.ForEachInRange(src.MapId, target.X, target.Y, splash, EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id.Value == target.Id.Value) continue;
            if (v.Id.Value == src.Id.Value) continue;
            _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, v, SkillId, skillLevel);
        }
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.Soundblend);
    }
}
