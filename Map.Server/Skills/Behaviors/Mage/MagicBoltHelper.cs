using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// Shared per-hit damage formula for the Mage bolt family
/// (MG_FIREBOLT / MG_COLDBOLT / MG_LIGHTNINGBOLT). Mirrors the
/// helper inlined in rathena-fork's per-bolt skill files: each
/// bolt hit deals approximately the caster's MATK midpoint as
/// damage, with the element fix applied downstream by the
/// damage pipeline.
/// </summary>
internal static class MagicBoltHelper
{
    public static int PerHitDamage(Entity src)
    {
        var matk = (src.Stats.MatkMin + src.Stats.MatkMax) / 2;
        return Math.Max(1, matk);
    }
}
