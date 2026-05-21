using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_POTIONPITCHER — Alchemist Aid Potion (Potion Pitcher). Manual
/// port of <c>rathena-fork/src/map/skills/merchant/aidpotion.cpp</c>.
///
/// <para>Drops a potion onto an ally to heal HP/SP. The full path
/// runs the inventory potion's <c>script</c> (potion_hp, potion_sp)
/// scaled by AM_POTIONPITCHER + AM_LEARNINGPOTION passive levels +
/// the target's VIT/INT + SM_RECOVERY/MG_SRECOVERY bonuses. None of
/// those are wired here yet — we fall back to the rAthena "monster
/// caster" healing curve based on skill level.</para>
/// </summary>
public sealed class AidPotion : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;
    private readonly Random _rng;

    public AidPotion() : base(SkillIds.AM_POTIONPITCHER) => _rng = Random.Shared;

    public AidPotion(IStatusOpsService? statusOps = null, Random? rng = null) : base(SkillIds.AM_POTIONPITCHER)
    {
        _statusOps = statusOps;
        _rng = rng ?? Random.Shared;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Monster-cast curve: base HP by skill level then VIT scale.
        var hp = skillLevel switch
        {
            1 => 45,
            2 => 105,
            3 => 175,
            _ => 325,
        };
        hp = (hp + _rng.Next(skillLevel * 20 + 1)) * (150 + skillLevel * 10) / 100;
        hp = hp * (100 + target.Stats.Vit * 2) / 100;
        _statusOps?.Heal(target, hp, 0, 0);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
