using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// rAthena <c>pc_addfame</c> (pc.cpp:441) — adds fame points to the
/// character. Used by:
/// <list type="bullet">
///   <item>Blacksmith — Forge Weapon procs.</item>
///   <item>Alchemist — Brew Potion (rare hit).</item>
///   <item>Taekwon — Ranker drops.</item>
///   <item>PvP/WoE — guild kill bounties.</item>
/// </list>
/// Top-N rankings (the fame list) are aggregated at char-server
/// boot; we expose the increment-side helper here.
/// </summary>
public interface IPlayerFameService
{
    /// <summary>Add <paramref name="count"/> fame to the caller. Returns the new total.</summary>
    int AddFame(PlayerEntity pc, int count);
}
