namespace Map.Server.Party;

/// <summary>
/// Party minimap-dot + HP-bar sync. Port of rAthena <c>party_send_xy_timer</c> (party.cpp:1177) +
/// <c>clif_party_hp</c> — a coarse periodic broadcast (default ~1 s, <c>battle_config.party_update_interval</c>)
/// that pushes each online party member's position (ZC_NOTIFY_POSITION_TO_GROUPM) and HP
/// (ZC_NOTIFY_HP_TO_GROUPM) to their same-map party members when either changed.
/// </summary>
public interface IPartySyncService
{
    /// <summary>Called from the map-server game loop; self-gates to the party-update cadence.</summary>
    void Tick(long nowTick);
}
