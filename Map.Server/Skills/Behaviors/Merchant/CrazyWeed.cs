using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_CRAZYWEED — Genetic Crazy Weed. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/crazyweed.cpp</c>.
/// Drops 3 + lv/2 random plant-strike attacks staggered 150 ms apart.
/// Per-strike GN_CRAZYWEED_ATK isn't on our SkillIds catalog — we
/// schedule the main-skill hit per stagger instead.
/// </summary>
public sealed class CrazyWeed : SkillImpl
{
    private readonly ISkillTimerService? _timers;
    private readonly ISkillAttackService? _skillAttack;

    public CrazyWeed() : base(SkillIds.GN_CRAZYWEED) { }

    public CrazyWeed(ISkillTimerService? timers = null, ISkillAttackService? skillAttack = null) : base(SkillIds.GN_CRAZYWEED)
    {
        _timers = timers;
        _skillAttack = skillAttack;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var hits = 3 + skillLevel / 2;
        for (var i = 0; i < hits; i++)
        {
            _timers?.Schedule(src, src, i * 150, SkillId, skillLevel,
                (s, t, lv) => _skillAttack?.SkillAttack(BattleAttackType.Weapon, s, s, t, SkillId, lv));
        }
    }
}
