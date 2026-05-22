using Map.Server.Entities;

namespace Map.Server.Mob.Conditions;

/// <summary>
/// rAthena MSC_MASTERATTACKED (mob.cpp:4373-4374) —
/// <c>md->master_id > 0 &amp;&amp; (fbl=map_id2bl(master_id))
/// &amp;&amp; unit_counttargeted(fbl) > 0</c>.
///
/// <para>Fires when the slave/summon's master is currently being
/// attacked by at least one entity. Lets summoned mobs cast a
/// protective skill (e.g. Cannibalize, Sphere shield) when the
/// owner takes damage.</para>
///
/// <para>Implementation: we look up <see cref="MobEntity.MasterId"/>
/// in the registry and consult the master's distinct-attacker count.
/// For mob masters we read <see cref="MobEntity.DmgList"/>.
/// PC masters (homunculus / mercenary owners) are a data-pending
/// branch — there's no <c>unit_counttargeted</c> on
/// <see cref="PlayerEntity"/> yet — so we return false in that case
/// rather than spuriously firing.</para>
/// </summary>
public sealed class MasterAttackedCondition : IMobSkillConditionEvaluator
{
    public MobSkillCondition Kind => MobSkillCondition.MasterAttacked;

    public bool IsMet(MobEntity mob, MobSkillEntry entry, MobConditionContext context)
    {
        if (mob.MasterId == null || context.Entities == null) return false;
        var master = context.Entities.Get(mob.MasterId.Value);
        if (master == null) return false;

        return master switch
        {
            MobEntity mm => mm.DmgList.DistinctAttackerCount > 0,
            // T5.1a — PC masters (homunculus / mercenary owners) read
            // through the same DmgList shape via PlayerEntity.AttackerLog,
            // populated by IDamageService on every incoming hit.
            PlayerEntity pm => pm.AttackerLog.DistinctAttackerCount > 0,
            _ => false,
        };
    }
}
