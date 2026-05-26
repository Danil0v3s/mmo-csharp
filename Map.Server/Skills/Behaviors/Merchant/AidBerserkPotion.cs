using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_BERSERKPITCHER — Alchemist Aid Berserk Potion. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/aidberserkpotion.cpp</c>.
/// Same path as <see cref="AidPotion"/> — NPC fallback heal: base
/// formula <c>(hp + rnd(lv*20+1)) * (150 + lv*10) / 100</c>, scaled by
/// <c>(100 + tgt_VIT*2) / 100</c>.
///
/// <para>rAthena's PC path additionally rejects the cast when the
/// target's <c>base_level &lt; inventory_data[require.itemid[x]]-&gt;elv</c>
/// — the consumed potion's equip-level gate. 🚩 INFRA-DEFERRED until
/// <c>skill_get_requirement</c> item-cost lookup is plumbed through
/// <see cref="ISkillRequirementService"/>; without that we can't
/// resolve which item is being expended (lv-dependent slot picker
/// <c>x = lv%11 - 1</c>) or its <c>elv</c>.</para>
/// </summary>
public sealed class AidBerserkPotion : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;
    private readonly Random _rng;

    public AidBerserkPotion() : base(SkillIds.AM_BERSERKPITCHER) => _rng = Random.Shared;

    public AidBerserkPotion(IStatusOpsService? statusOps = null, Random? rng = null) : base(SkillIds.AM_BERSERKPITCHER)
    {
        _statusOps = statusOps;
        _rng = rng ?? Random.Shared;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
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
