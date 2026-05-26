using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// Shared per-hit damage formula for the Mage bolt family
/// (MG_FIREBOLT / MG_COLDBOLT / MG_LIGHTNINGBOLT). Mirrors the
/// helper inlined in rathena-fork's per-bolt skill files: each
/// bolt hit deals approximately the caster's MATK midpoint as
/// damage, with the element fix applied downstream by the
/// damage pipeline.
///
/// <para>Honors rAthena bolt SC modifiers from
/// <c>skills/mage/firebolt.cpp</c> and siblings:
/// <c>SC_FLAMETECHNIC_OPTION</c> multiplies per-hit by 5×, and the
/// SC_SPELLFIST modifier (when miscflag is short-range melee) bumps
/// the ratio by <c>(spellfist_val3·100 + spellfist_val1·50 − 150)%</c>.</para>
/// </summary>
internal static class MagicBoltHelper
{
    public static int PerHitDamage(Entity src) => PerHitDamage(src, sc: null);

    public static int PerHitDamage(Entity src, IStatusChangeService? sc)
    {
        var matk = (src.Stats.MatkMin + src.Stats.MatkMax) / 2;
        if (matk < 1) matk = 1;

        if (sc != null)
        {
            // SC_FLAMETECHNIC_OPTION — base damage × 5 (Sorcerer elemental boost).
            if (sc.Get(src, StatusType.FlametechnicOption) != null)
                matk *= 5;
            // SC_SPELLFIST — Sage spellfist next-cast bolt redirected to short-range.
            // val3 stores the absorbed-bolt level, val1 stores the spellfist
            // level itself; bonus % = val3·100 + val1·50 − 150 (above the
            // baseline 100 % at level 0). We apply this as a flat multiplier
            // on the MATK midpoint matching rAthena's intent — the engine
            // doesn't yet supply BF_SHORT/BF_LONG gating to bolts, so we
            // apply unconditionally; SC consumption stays the consumer's
            // job.
            var spellfist = sc.Get(src, StatusType.Spellfist);
            if (spellfist != null && spellfist.Val1 > 0)
            {
                int bonusPct = spellfist.Val3 * 100 + spellfist.Val1 * 50 - 150;
                if (bonusPct > 0) matk += matk * bonusPct / 100;
            }
        }

        return matk;
    }
}
