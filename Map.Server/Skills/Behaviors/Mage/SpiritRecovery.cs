using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_EL_CURE — Sorcerer Spirit Recovery / Elemental Cure
/// (skill.cpp:SO_EL_CURE arm). Refuses if the caster has no bound
/// elemental (<see cref="PlayerEntity.ActiveElementalClassId"/> = 0).
/// On success drains 10 % of the caster's current HP+SP and hands the
/// healed amount to the elemental via
/// <see cref="Map.Server.Elemental.IElementalService.Heal"/>.
/// </summary>
public sealed class SpiritRecovery : SkillImpl
{
    public SpiritRecovery() : base(SkillIds.SO_EL_CURE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        if (pc.ActiveElementalClassId == 0)
        {
            ctx.Client?.BroadcastSkillFail(pc, SkillId,
                Core.Server.Packets.Out.ZC.SkillFailCause.SummonNone);
            return;
        }
        var hpTransfer = pc.Hp / 10;
        var spTransfer = pc.Sp / 10;
        pc.Hp = Math.Max(1, pc.Hp - hpTransfer);
        pc.Sp = Math.Max(0, pc.Sp - spTransfer);
        ctx.Elemental?.Heal(pc, hpTransfer, spTransfer);
    }
}
