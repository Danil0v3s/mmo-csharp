using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// Port of rAthena <c>pc_scdata_received</c> (pc.cpp:14747). Called after
/// the char-server returns the saved <c>sc_data</c> table for a player at
/// session enter. Drives the post-load chain:
/// <list type="number">
///   <item>Decode the SC bytes via <see cref="ScDataCodec"/>.</item>
///   <item>Re-apply each persisted SC via <see cref="IStatusChangeService.Start"/>
///         (skips rows whose duration has already elapsed).</item>
///   <item>Rental expiration sweep (<c>pc_inventory_rentals</c>,
///         pc.cpp:1187 — runs after SCs because some persisted rentals
///         attach to SCs).</item>
///   <item>Mark the session as fully loaded
///         (rAthena's <c>sd->state.pc_loaded = true</c> latch).</item>
/// </list>
///
/// <para>The 4th-class SC restoration is gated on YAML data
/// (db/re/status.yml) currently lacking some 4th-class entries — those
/// types decode but their handlers default to presence-marker shape.
/// Once the YAML lands, behavior is unchanged on the entry point.</para>
/// </summary>
public interface IPlayerScDataReceivedService
{
    /// <summary>rAthena <c>pc_scdata_received</c> entry point. Pass the
    /// `bytes data` field from the char-server `StatusChangeDataResponse`.</summary>
    void OnReceived(PlayerEntity pc, byte[] scDataPayload);
}
