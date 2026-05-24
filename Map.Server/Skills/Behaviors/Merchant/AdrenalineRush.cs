using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BS_ADRENALINE — Blacksmith Adrenaline Rush. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/adrenalinerush.cpp</c>.
/// rAthena starts SC_ADRENALINE with val1 = skill level, val2 = 1 when
/// the cast is on self (used by the SC engine to suppress double-apply
/// on the party splash). Same-map party fan-out matches
/// <see cref="WeaponPerfection"/>; the axe/mace weapontype gate is
/// applied per-recipient via <see cref="PlayerEntity.WeaponType"/>.
/// </summary>
public sealed class AdrenalineRush : SkillImpl
{
    // rAthena skill_get_weapontype(BS_ADRENALINE) → axe (1<<6) | mace (1<<8).
    private const int WeaponMaskAxeMace = (1 << 6) | (1 << 8);

    public AdrenalineRush() : base(SkillIds.BS_ADRENALINE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ApplyTo(target, skillLevel, src, ctx, isSelf: src.Id == target.Id);
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);

        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ApplyTo(m, skillLevel, src, ctx, isSelf: false);
            }, includeSelf: false);
        }
    }

    private static void ApplyTo(Entity bl, ushort skillLevel, Entity src, SkillBehaviorContext ctx, bool isSelf)
    {
        // rAthena pc_check_weapontype: only land on recipients wielding axe/mace.
        if (bl is PlayerEntity pc && (pc.WeaponType & WeaponMaskAxeMace) == 0) return;
        ctx.Sc?.Start(bl, StatusType.Adrenaline, val1: skillLevel, val2: isSelf ? 1 : 0, 0, 0, durationMs: 150_000, src);
    }
}
