using Map.Server.Entities;

namespace Map.Server.Mob;

/// <summary>
/// Port of rAthena <c>mob_warpchase</c> (mob.cpp:1776). When a mob's
/// target sits on another map, the AI scans for the nearest warp NPC
/// or warp skill-unit whose exit lands on the target's map, then
/// walks toward that warp.
///
/// <para>Gated by <c>battle_config.mob_ai &amp; 0x40</c> (disabled
/// when bit is unset) and <c>battle_config.mob_warp</c>:
/// <list type="bullet">
///   <item><c>&amp; 1</c> — chase BL_NPC warp portals.</item>
///   <item><c>&amp; 2</c> — chase BL_SKILL warps (Priest Warp Portal).</item>
///   <item><c>&amp; 4</c> — refuse warps that lead to MF_NOBRANCH maps.</item>
/// </list>
/// </para>
/// </summary>
public interface IMobWarpChaseService
{
    /// <summary>
    /// Try to warp-chase <paramref name="target"/> from
    /// <paramref name="mob"/>'s position.
    /// </summary>
    /// <returns>The action the AI ticker should take next tick.</returns>
    WarpChaseResult TryWarpChase(MobEntity mob, Entity target);
}

public enum WarpChaseResult
{
    /// <summary>rAthena return code 0 — no chase performed
    /// (disabled, no warp found, same map / already in range).</summary>
    NotApplicable,
    /// <summary>rAthena return code 1 — walk-to-warp queued.</summary>
    Walking,
    /// <summary>rAthena return code 2 — already on a path to a warp NPC.</summary>
    AlreadyChasing,
}
