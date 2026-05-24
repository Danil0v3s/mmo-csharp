using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_ANKOKURYUUAKUMU — Dark Dragon Nightmare. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/darkdragonnightmare.cpp</c>.
/// Splash damage; ratio <c>+(-100 + 15500*lv) + 5*spl</c>. Ends
/// SC_NIGHTMARE on hit and adds a second hit if the target already
/// had SC_NIGHTMARE.
/// </summary>
public sealed class DarkDragonNightmare : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public DarkDragonNightmare() : base(SkillIds.SS_ANKOKURYUUAKUMU) { }

    public DarkDragonNightmare(ISkillAttackService? skillAttack = null) : base(SkillIds.SS_ANKOKURYUUAKUMU)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 15500 * skillLevel) + 5 * src.Stats.Spl;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // rAthena map_foreachinrange( skill_area_sub … BCT_ENEMY | SD_SPLASH | 1 ).
        var attack = _skillAttack ?? ctx.SkillAttack;
        attack?.SkillAttackArea(src, target, SkillId, skillLevel);
        if (ctx.Sc?.Get(target, StatusType.Nightmare) != null)
        {
            attack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
        }
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.End(target, StatusType.Nightmare);
}
