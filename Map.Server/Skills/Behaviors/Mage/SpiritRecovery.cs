using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_EL_CURE — Sorcerer Spirit Recovery (Elemental Cure). Manual port of
/// <c>rathena-fork/src/map/skills/mage/spiritrecovery.cpp</c>.
///
/// <para>Trades 10 % of the caster's current HP+SP to heal the bound
/// elemental by the same amount. Bound-elemental access isn't wired
/// here yet — for now we validate the caster type and broadcast.</para>
/// </summary>
public sealed class SpiritRecovery : SkillImpl
{
    public SpiritRecovery() : base(SkillIds.SO_EL_CURE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: charge sd 10 % HP/SP, heal sd.BoundElemental by the same.
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
