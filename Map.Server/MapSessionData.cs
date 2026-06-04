using System.Net.Sockets;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server;

public class MapSessionData(
    Socket socket,
    int heartbeatTimeout,
    IPacketFactory packetFactory,
    IPacketSizeRegistry sizeRegistry,
    ILogger logger
) : ClientSession(socket, heartbeatTimeout, packetFactory, sizeRegistry, logger)
{
    /// <summary>Auth lifecycle — see <see cref="MapAuthState"/>.</summary>
    public MapAuthState AuthState { get; set; } = MapAuthState.Unauthenticated;

    public int? AccountId { get; set; }
    public int? CharacterId { get; set; }
    public int? LoginId1 { get; set; }
    public byte Sex { get; set; }

    /// <summary>
    /// Account group id (rAthena <c>account_data.group_id</c>). Drives GM
    /// command authorization in <c>ChatCommandHandler</c>; 0 = no GM
    /// privileges. Populated from <c>CharacterMapAuthResponse.group_id</c>
    /// on auth.
    /// </summary>
    public uint GroupId { get; set; }

    /// <summary>
    /// Internal mapId (hash of map name) the player is currently bound to.
    /// Set when the auth ticket resolves; the spawn happens on
    /// <c>CZ_NOTIFY_ACTORINIT</c>.
    /// </summary>
    public uint? MapId { get; set; }
    public short SpawnX { get; set; }
    public short SpawnY { get; set; }
    public byte SpawnDir { get; set; }
    public string? CharacterName { get; set; }

    /// <summary>
    /// The full character snapshot received from char server during
    /// <c>CZ_WANT_TO_CONNECTION</c>. Cached so the LoadEndAck cascade
    /// (<see cref="Map.Server.Handlers.NotifyActorInitHandler"/>) can
    /// reuse it without a second IPC roundtrip. Cleared at session
    /// teardown.
    /// </summary>
    public CharacterDataResponse? CharacterData { get; set; }

    /// <summary>
    /// Block-list id of the live <see cref="PlayerEntity"/>, populated once
    /// the player is registered in <see cref="IEntityRegistry"/>. Used by the
    /// disconnect path to find and tear down the entity.
    /// </summary>
    public EntityId? EntityId { get; set; }

    /// <summary>
    /// True once the disconnect cleanup has run. Idempotency guard so the
    /// periodic lifecycle sweep doesn't double-broadcast vanish.
    /// </summary>
    public bool CleanupCompleted { get; set; }

    /// <summary>
    /// Active NPC dialog state, set by the scripting dispatcher between
    /// <c>CZ_CONTACTNPC</c> and the final <c>CZ_CLOSE_DIALOG</c>. Null when
    /// the player is not in a dialog.
    /// </summary>
    public Map.Server.Scripting.Dialog.DialogSession? Dialog { get; set; }

    /// <summary>
    /// Memory-only variable bag for the script-side <c>ctx.player.session</c>
    /// scope (rAthena <c>@var</c>). Allocated lazily on first script access;
    /// lifetime is tied to the session, reset on disconnect.
    /// </summary>
    public Microsoft.ClearScript.PropertyBag? ScriptSessionVars { get; set; }

    /// <summary>
    /// Persistent variable-register scopes (perm / account / accountGlobal)
    /// loaded from the DB during the connect flow. Saved on autosave +
    /// disconnect via <c>IPlayerStateService.SaveAsync</c>. Null until
    /// the load completes.
    /// </summary>
    public Map.Server.Persistence.PlayerVarRegs? VarRegs { get; set; }

    /// <summary>
    /// COMBAT-48 — the character's Warp Portal memo points (rAthena
    /// <c>memo_point[3]</c>), loaded from the <c>memo</c> table by
    /// <c>IPlayerStateService.LoadAsync</c>. Copied onto
    /// <c>PlayerEntity.MemoPoints</c> when the entity is built
    /// (NotifyActorInitHandler) and persisted back by <c>SaveAsync</c>.
    /// Empty <c>MapName</c> = empty slot. Order = save order (memo id).
    /// </summary>
    public (string MapName, short X, short Y)[] MemoPoints { get; set; }
        = new (string, short, short)[3];

    /// <summary>
    /// Per-character inventory loaded by <c>IInventoryService.LoadAsync</c>
    /// during the connect flow. Null until the load completes; empty list
    /// for fresh characters with no items. Order matches the server-side
    /// inventory slot index used in client packets.
    /// </summary>
    public List<Map.Server.Inventory.InventoryItem>? Inventory { get; set; }

    /// <summary>
    /// DB row Ids that were dropped from <see cref="Inventory"/> since the
    /// last save (item consumption, drop-on-ground, trade-out, etc.). The
    /// next <c>SaveAsync</c> issues <c>DELETE</c> for each Id and clears
    /// this list. Items added since load have <c>Id == 0</c> on their
    /// runtime <c>InventoryItem</c> and get <c>INSERT</c>ed; once saved,
    /// EF populates the generated Id back on the runtime object.
    /// </summary>
    public List<int> RemovedInventoryIds { get; } = new();

    /// <summary>
    /// COMBAT-94 — rAthena <c>clif_delitem</c> (<c>ZC_DELETE_ITEM_FROM_BODY</c>, 0x07fa): tell the
    /// owning client to decrement the shown stack at <paramref name="serverIndex"/> by
    /// <paramref name="count"/> immediately on a consume. The shared delitem helper for every
    /// partial-consume path (ammo, item use, requirement payment) — a full-slot removal still rides
    /// the periodic <see cref="RemovedInventoryIds"/> sync. <paramref name="reason"/> is the rAthena
    /// delete-type (0 = Normal, 1 = used for a skill). No-op for a zero count.
    /// </summary>
    public void NotifyItemConsumed(int serverIndex, uint count, byte reason)
    {
        if (count == 0) return;
        EnqueuePacket(new Core.Server.Packets.Out.ZC.ZC_DELETE_ITEM_FROM_BODY
        {
            DeleteType = reason,
            Index = (ushort)(serverIndex + 2),       // client inventory index = server slot + 2
            Amount = (ushort)System.Math.Min(count, ushort.MaxValue),
        });
    }

    /// <summary>
    /// Per-character cart inventory loaded alongside the regular
    /// inventory. Null until <c>pc_setcart</c> activates one;
    /// remains null for characters without the cart skill. Items
    /// here are still <see cref="Map.Server.Inventory.InventoryItem"/> rows but
    /// stored under <c>cart_inventory</c> in MySQL (the existing
    /// <c>CartInventoryRepository</c>).
    /// </summary>
    public List<Map.Server.Inventory.InventoryItem>? Cart { get; set; }

    /// <summary>
    /// Per-session autoloot configuration. rAthena <c>sd-&gt;state.autoloot</c>
    /// is a global %-chance gate; <c>sd-&gt;state.autoloottype</c> is a
    /// bitmask of item types to grab; <c>sd-&gt;state.autolootid[]</c> is
    /// a list of up to 10 whitelisted item ids.
    /// </summary>
    public byte AutolootRate { get; set; }
    public uint AutolootTypeMask { get; set; }
    public readonly List<uint> AutolootItemIds = new();

    /// <summary>
    /// Live account-storage state while a kafra/storage window is open.
    /// Null until <see cref="Map.Server.Storage.IStorageService.OpenAsync"/> hydrates.
    /// </summary>
    public Map.Server.Storage.StorageState? Storage { get; set; }

    /// <summary>
    /// Live player-trade state while a deal is in progress. Null when
    /// not in a trade. Mirrors the rAthena <c>sd-&gt;trade_partner +
    /// sd-&gt;state.trading + sd-&gt;deal</c> bundle.
    /// </summary>
    public Map.Server.Trade.TradeState? Trade { get; set; }

    /// <summary>
    /// Currently-open NPC shop's entity id, or null when not shopping.
    /// Mirrors rAthena <c>sd-&gt;npc_shopid</c>. Set when the buy/sell
    /// list packet goes out, cleared on result.
    /// </summary>
    public int? OpenShopNpcId { get; set; }

    /// <summary>
    /// True while the player has the cash-shop UI open (the "cash shop"
    /// button path). Mirrors rAthena <c>sd-&gt;state.cashshop_open</c> —
    /// set by the open request, cleared by close.
    /// </summary>
    public bool CashShopOpen { get; set; }
}
