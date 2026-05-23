using Map.Server.Entities;
using Map.Server.Inventory.Script;
using Map.Server.Items;
using Map.Server.Scripting;
using Map.Server.Scripting.Dialog;
using Map.Server.Scripting.Records;
using Map.Server.Session;
using Map.Server.Status;
using Microsoft.ClearScript;

namespace Map.Server.Inventory;

/// <summary>
/// Default implementation. Pulls the relevant <see cref="ItemRegistration"/>
/// from the shared <see cref="INpcRegistry"/> (populated at boot from
/// <c>scripts/items/</c>), constructs the right host context, and
/// invokes via <c>__invokeHookWithCtx</c> for the Proxy fallback —
/// unknown method calls return 0 instead of throwing, matching combo
/// dispatch (CONV-3) and runtime DSL (DSL-4) semantics.
///
/// <para>
/// OnUse uses the rich <see cref="PlayerContext"/> + <see cref="WorldContext"/>
/// from NPC dialogs so item-use hooks share the same authoring surface.
/// OnEquip / OnUnequip use <see cref="ScriptedBonusHost"/> — same surface
/// the combo dispatcher uses; bundle mutations land in-place.
/// </para>
/// </summary>
public sealed class ItemHookDispatcher : IItemHookDispatcher
{
    private readonly ScriptHost _scriptHost;
    private readonly INpcRegistry _registry;
    private readonly IItemCatalog _catalog;
    private readonly IInventoryService _inventory;
    private readonly IPlayerBonusService? _bonusSvc;
    private readonly IEntityRegistry? _entities;
    private readonly ILogger<ItemHookDispatcher> _logger;

    public ItemHookDispatcher(
        ScriptHost scriptHost,
        INpcRegistry registry,
        IItemCatalog catalog,
        IInventoryService inventory,
        ILogger<ItemHookDispatcher> logger,
        IPlayerBonusService? bonusSvc = null,
        IEntityRegistry? entities = null)
    {
        _scriptHost = scriptHost;
        _registry = registry;
        _catalog = catalog;
        _inventory = inventory;
        _bonusSvc = bonusSvc;
        _entities = entities;
        _logger = logger;
    }

    public bool TryInvokeOnUse(MapSessionData session, PlayerEntity player, InventoryItem item)
    {
        var reg = _registry.GetItemById((int)item.NameId);
        if (reg?.Hooks.OnUse is not { } handle) return false;

        var row = _catalog.Get(item.NameId);
        var ctx = new ItemUseHostContext(
            new PlayerContext(session, player, _inventory),
            new WorldContext(),
            new ItemUseItemInfo(
                id: (int)item.NameId,
                nameAegis: row?.NameAegis ?? "",
                refine: item.Refine,
                slot: item.Equip != 0 ? 1 : 0, // placeholder — slot mapping a future wave
                amount: (int)item.Amount));

        try
        {
            var invoker = _scriptHost.Engine.Script.__invokeHookWithCtx;
            var result = invoker(handle.Value, ctx);
            // onUse returns a Promise (TS shape is `async (ctx) => ...`);
            // attach a fault handler so script exceptions land in logs.
            if (result is Task t)
            {
                t.ContinueWith(c =>
                {
                    if (c.IsFaulted)
                    {
                        _logger.LogError(c.Exception,
                            "Item {Id} onUse threw for char {Char}",
                            item.NameId, player.CharacterId);
                    }
                }, TaskScheduler.Default);
            }
        }
        catch (ScriptEngineException ex)
        {
            _logger.LogWarning(ex,
                "Item {Id} onUse throw at invocation: {Details}",
                item.NameId, ex.ErrorDetails);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Item {Id} onUse throw at invocation for char {Char}",
                item.NameId, player.CharacterId);
        }
        return true;
    }

    public bool TryInvokeOnEquip(
        InventoryItem item, EquipBonusBundle bundle, PlayerEntity player,
        IReadOnlyList<InventoryItem> equipped)
    {
        var reg = _registry.GetItemById((int)item.NameId);
        if (reg?.Hooks.OnEquip is not { } handle) return false;
        InvokeBonusHook(handle, item.NameId, bundle, player, equipped, hookName: "onEquip");
        return true;
    }

    public void TryInvokeOnUnequip(
        InventoryItem item, EquipBonusBundle bundle, PlayerEntity player,
        IReadOnlyList<InventoryItem> equipped)
    {
        var reg = _registry.GetItemById((int)item.NameId);
        if (reg?.Hooks.OnUnequip is not { } handle) return;
        InvokeBonusHook(handle, item.NameId, bundle, player, equipped, hookName: "onUnequip");
    }

    /// <summary>
    /// Shared invocation path for the two sync hooks (onEquip / onUnequip).
    /// Builds a <see cref="ScriptedBonusHost"/> ctx and pushes it through
    /// the same <c>__invokeHookWithCtx</c> helper combo dispatch uses.
    /// </summary>
    private void InvokeBonusHook(
        ScriptHandle handle, uint itemId, EquipBonusBundle bundle, PlayerEntity player,
        IReadOnlyList<InventoryItem> equipped, string hookName)
    {
        var host = new ScriptedBonusHost(player, bundle, equipped, _catalog, _bonusSvc, _entities);
        try
        {
            var invoker = _scriptHost.Engine.Script.__invokeHookWithCtx;
            invoker(handle.Value, host);
        }
        catch (ScriptEngineException ex)
        {
            _logger.LogDebug(ex,
                "Item {Id} {Hook} threw: {Details}",
                itemId, hookName, ex.ErrorDetails);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Item {Id} {Hook} threw", itemId, hookName);
        }
    }
}
