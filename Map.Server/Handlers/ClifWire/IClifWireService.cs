using Map.Server.Entities;

namespace Map.Server.Handlers.ClifWire;

/// <summary>
/// Outbound-packet builders. Canonical entry points for the rAthena
/// <c>clif.cpp</c> public surface (25 817 lines, 780 enumerated
/// public functions — almost all of them are `clif_*` packet emitters
/// like <c>clif_charnameack</c>, <c>clif_party_xy</c>, etc.).
///
/// The C# port handles outbound packets through
/// <see cref="Core.Server.Packets.OutgoingPacket"/> + per-packet
/// emitter classes. This service is the *canonical naming seam* —
/// rAthena consumers (skills / scripts / atcommands) that say
/// "send a clif_status_change to X" have a single named method to
/// call.
///
/// The interface is intentionally minimal — most clif_* methods are
/// either:
/// <list type="bullet">
///   <item>already covered by a dedicated `Send*Packet` helper, or</item>
///   <item>fired by a packet emitter that owns its own send loop.</item>
/// </list>
/// New entry points get added when the matching consumer (the skill
/// system, the chat router, the trade engine) needs one.
/// </summary>
public interface IClifWireService
{
    /// <summary>rAthena <c>clif_messagecolor</c> — colored system message.</summary>
    void MessageColor(PlayerEntity pc, uint colorRgb, string text);

    /// <summary>
    /// rAthena <c>mob_chat_display_message</c> (mob.cpp:4205) — emits
    /// a colored chat line attached to <paramref name="mob"/> to every
    /// PC in AREA_CHAT_WOC. Format mirrors rAthena: <c>"&lt;name&gt; : &lt;text&gt;"</c>
    /// (the leading "#" identifier suffix on the mob name is stripped).
    /// </summary>
    void MobChat(Entities.MobEntity mob, uint colorRgb, string text);

    /// <summary>
    /// T5.3b — rAthena <c>clif_status_change</c> (clif.cpp:9852) +
    /// <c>clif_efst_status_change</c>. Broadcasts the SC icon on/off
    /// frame (ZC_MSG_STATE_CHANGE / ZC_EFST_STATUS_CHANGE) to AOI so
    /// every nearby PC's HUD shows the buff/debuff icon over the target.
    /// Fires from <see cref="Map.Server.Status.IStatusChangeService.Start"/> /
    /// <see cref="Map.Server.Status.IStatusChangeService.End"/>.
    /// </summary>
    /// <param name="target">Entity carrying the SC.</param>
    /// <param name="type">SC id (mapped to rAthena SC_*).</param>
    /// <param name="active">True for SC start (icon on), false for SC end (icon off).</param>
    /// <param name="totalMs">Total SC duration in ms (only meaningful when active).</param>
    /// <param name="val1">Per-SC value 1 (e.g. ATK% boost amount).</param>
    /// <param name="val2">Per-SC value 2.</param>
    /// <param name="val3">Per-SC value 3.</param>
    void StatusChange(Entities.Entity target, Map.Server.Status.StatusType type, bool active,
        int totalMs = 0, int val1 = 0, int val2 = 0, int val3 = 0);

    /// <summary>
    /// T5.3d — rAthena <c>clif_spawn(pet/homun/merc/elem)</c> family.
    /// Broadcasts a companion entity entering AOI. Distinct from the
    /// generic mob/PC spawn since the wire packets carry the master
    /// relationship for color-name tinting + the pet-egg / homun-form
    /// flags.
    /// </summary>
    void CompanionSpawn(Entities.Entity companion, Entities.Entity master);

    /// <summary>
    /// T5.3d — rAthena <c>clif_clearchar_skillunit</c> family for
    /// companion despawn (master unsummons, companion dies, or master
    /// disconnects). Broadcasts to AOI so onlookers stop rendering.
    /// </summary>
    void CompanionVanish(Entities.Entity companion);

    /// <summary>
    /// T5.3d — rAthena <c>clif_send_homdata</c> / <c>clif_pet_food</c>
    /// level-up frame. Updates the pet hunger / homun intimacy / level
    /// HUD on the master's client. Self-only (companion HUDs are
    /// caster-private).
    /// </summary>
    void CompanionLevelUp(Entities.PlayerEntity master, Entities.Entity companion, int newLevel);

    /// <summary>
    /// T5.3e — rAthena <c>clif_inventorylist</c> / <c>clif_cart_list</c> /
    /// <c>clif_storage_list</c> full serialization. Sent on map enter,
    /// cart toggle, storage open, equip swap (changes invalidate
    /// per-slot state). The per-slot incremental packets are still
    /// emitted by individual handlers (this is the all-at-once dump).
    /// </summary>
    /// <param name="owner">PC the list is being sent to (caster-only).</param>
    /// <param name="kind">Inventory / Cart / Storage / GuildStorage.</param>
    void InventoryList(Entities.PlayerEntity owner, InventoryListKind kind);

    /// <summary>rAthena <c>clif_displaymessage</c> — single-line message in chat.</summary>
    void DisplayMessage(PlayerEntity pc, string text);

    /// <summary>rAthena <c>clif_broadcast</c> — server-wide announcement.</summary>
    void Broadcast(string text, uint colorRgb, byte type);

    /// <summary>rAthena <c>clif_broadcast2</c> — map-scoped announcement.</summary>
    void Broadcast2(uint mapId, string text, uint colorRgb, byte type);

    /// <summary>rAthena <c>clif_refresh</c> — re-send the full PC state to its own client.</summary>
    void Refresh(PlayerEntity pc);

    /// <summary>rAthena <c>clif_changemap</c> — map-warp packet.</summary>
    void ChangeMap(PlayerEntity pc, string mapName, short x, short y);

    /// <summary>rAthena <c>clif_clearunit_single</c> — despawn a single entity from one client.</summary>
    void ClearUnitSingle(EntityId targetId, byte type, PlayerEntity audience);

    /// <summary>rAthena <c>clif_clearunit_area</c> — despawn an entity from everyone in range.</summary>
    void ClearUnitArea(Entity target, byte type);

    /// <summary>rAthena <c>clif_authok</c> — connection accepted.</summary>
    void AuthOk(PlayerEntity pc);

    /// <summary>rAthena <c>clif_authrefuse</c>.</summary>
    void AuthRefuse(int reason);

    /// <summary>rAthena <c>clif_authfail_fd</c>.</summary>
    void AuthFailFd(int fd, byte reason);
}

/// <summary>
/// Discriminator for <see cref="IClifWireService.InventoryList"/>.
/// Mirrors rAthena's distinct emitter functions.
/// </summary>
public enum InventoryListKind : byte
{
    Inventory = 0,
    Cart = 1,
    Storage = 2,
    GuildStorage = 3,
}
