using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_REMOVETRAP — Hunter Remove Trap. Manual port of
/// <c>rathena-fork/src/map/skills/archer/removetrap.cpp</c>.
///
/// <para>Picks up an own-laid trap (or any trap on Vs maps) and
/// refunds either the deploy item or the configured trap-back item.
/// Trap-unit lookup + item refund aren't surfaced to this hook — for
/// now we land the cast frame and emit a fail when the caster isn't
/// a player.</para>
/// </summary>
public sealed class RemoveTrap : SkillImpl
{
    public RemoveTrap() : base(SkillIds.HT_REMOVETRAP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd)
            return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Deferred: skill_unit lookup + skill_delunit + item refund (battle_config.skill_removetrap_type) — needs BL_SKILL targeting hook.
        _ = sd;
    }
}
