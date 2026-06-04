using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Session;
using Map.Server.Status;
using Microsoft.Extensions.DependencyInjection;

namespace Map.Server.Inventory;

/// <summary>
/// rAthena's INVTYPE_INVENTORY (0). Other values (cart, storage,
/// guild-storage) reuse the same packets with a different invType byte.
/// </summary>
internal static class InvType
{
    public const byte Inventory = 0;
}

public sealed class InventoryService : IInventoryService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IItemCatalog _itemCatalog;
    private readonly IEntityRegistry? _entityRegistry;
    private readonly IPlayerWeightStatusService? _weightStatus;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        IServiceScopeFactory scopeFactory,
        IItemCatalog itemCatalog,
        ILogger<InventoryService> logger,
        IEntityRegistry? entityRegistry = null,
        IPlayerWeightStatusService? weightStatus = null)
    {
        _scopeFactory = scopeFactory;
        _itemCatalog = itemCatalog;
        _entityRegistry = entityRegistry;
        _weightStatus = weightStatus;
        _logger = logger;
    }

    public async Task LoadAsync(MapSessionData session, CancellationToken cancellationToken = default)
    {
        if (session.CharacterId is not { } charId)
        {
            session.Inventory = new List<InventoryItem>();
            return;
        }

        // Scoped repo: the inventory DbSet is bound to a request-scoped
        // GameDbContext. Resolve in a fresh scope so we don't capture a
        // long-lived context on the session.
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();

        IReadOnlyList<InventoryEntity> rows;
        try
        {
            rows = await repo.GetByCharIdAsync(charId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to load inventory for char {CharId}; starting with empty bag",
                charId);
            session.Inventory = new List<InventoryItem>();
            return;
        }

        var list = new List<InventoryItem>(rows.Count);
        var slot = 0;
        foreach (var row in rows)
        {
            list.Add(new InventoryItem
            {
                Id = row.Id,
                ServerIndex = slot++,
                NameId = row.NameId,
                Amount = row.Amount,
                Equip = row.Equip,
                Identified = row.Identify != 0,
                Refine = row.Refine,
                Attribute = row.Attribute,
                Card0 = row.Card0,
                Card1 = row.Card1,
                Card2 = row.Card2,
                Card3 = row.Card3,
                Options = new[]
                {
                    new ItemOption { Id = row.OptionId0, Value = row.OptionVal0, Param = row.OptionParm0 },
                    new ItemOption { Id = row.OptionId1, Value = row.OptionVal1, Param = row.OptionParm1 },
                    new ItemOption { Id = row.OptionId2, Value = row.OptionVal2, Param = row.OptionParm2 },
                    new ItemOption { Id = row.OptionId3, Value = row.OptionVal3, Param = row.OptionParm3 },
                    new ItemOption { Id = row.OptionId4, Value = row.OptionVal4, Param = row.OptionParm4 },
                },
                ExpireTime = row.ExpireTime,
                Favorite = row.Favorite,
                Bound = row.Bound,
                UniqueId = row.UniqueId,
                EquipSwitch = row.EquipSwitch,
                EnchantGrade = row.EnchantGrade,
            });
        }
        session.Inventory = list;

        _logger.LogInformation("Loaded {Count} inventory rows for char {CharId}", list.Count, charId);
    }

    public void SendInventoryList(MapSessionData session)
    {
        if (session.Inventory is not { } items) return;

        // Split into stackable (normal) vs equippable per rAthena's
        // itemdb_isstackable2.
        var normals = new List<InventoryItem>();
        var equips  = new List<InventoryItem>();
        foreach (var item in items)
        {
            var entry = _itemCatalog.Get(item.NameId);
            var type = ItemTypeCodes.FromDbString(entry?.Type);
            if (ItemTypeCodes.IsEquippable(type)) equips.Add(item);
            else                                  normals.Add(item);
        }

        session.EnqueuePacket(new ZC_INVENTORY_START
        {
            InvType = InvType.Inventory,
            Name = string.Empty,
        });

        if (normals.Count > 0)
        {
            session.EnqueuePacket(new ZC_INVENTORYLIST_NORMAL_V6
            {
                InvType = InvType.Inventory,
                Body = InventoryPacketBuilder.BuildNormalListBody(normals, _itemCatalog),
            });
        }
        if (equips.Count > 0)
        {
            session.EnqueuePacket(new ZC_INVENTORYLIST_EQUIP_V6
            {
                InvType = InvType.Inventory,
                Body = InventoryPacketBuilder.BuildEquipListBody(equips, _itemCatalog),
            });
        }

        session.EnqueuePacket(new ZC_INVENTORY_END
        {
            InvType = InvType.Inventory,
            Flag = 0,
        });
    }

    public bool GiveItem(MapSessionData session, uint nameId, int amount)
    {
        if (amount <= 0) return false;
        var items = session.Inventory;
        if (items is null)
        {
            _logger.LogWarning(
                "GiveItem({NameId}, {Amount}) before LoadAsync ran — refusing", nameId, amount);
            return false;
        }

        var entry = _itemCatalog.Get(nameId);
        if (entry == null)
        {
            _logger.LogWarning("GiveItem: unknown item id {NameId}", nameId);
            return false;
        }
        var type = ItemTypeCodes.FromDbString(entry.Type);
        var equippable = ItemTypeCodes.IsEquippable(type);

        InventoryItem slot;
        if (!equippable)
        {
            // Stackable — merge into the first matching slot if one exists.
            // Match key for slice 4: nameid + identified + refine + cards +
            // attribute + options. Equip-switch / favorite / unique-id don't
            // affect stackability per rAthena.
            slot = items.FirstOrDefault(it => StacksWith(it, nameId)) ?? AppendNewSlot(items, nameId, type);
            slot.Amount = (uint)(slot.Amount + amount);
        }
        else
        {
            // Non-stackable — one slot per instance.
            slot = AppendNewSlot(items, nameId, type);
            slot.Amount = (uint)amount;
        }

        session.EnqueuePacket(new ZC_ITEM_PICKUP_ACK
        {
            Index = (short)(slot.ServerIndex + 2),
            Count = (short)amount,
            NameId = nameId,
            IsIdentified = (byte)(slot.Identified ? 1 : 0),
            IsDamaged = 0,
            Card0 = slot.Card0,
            Card1 = slot.Card1,
            Card2 = slot.Card2,
            Card3 = slot.Card3,
            Location = 0,  // equip-slot bits land with the Equipment slice
            Type = type,
            Result = 0,
            HireExpireDate = (int)slot.ExpireTime,
            BindOnEquipType = 0,
            Favorite = slot.Favorite,
            Look = 0,
            RefiningLevel = slot.Refine,
            EnchantGrade = slot.EnchantGrade,
        });

        _logger.LogInformation(
            "GiveItem: char {CharId} +{Amount}× {NameId} → slot {Slot} (now {Total})",
            session.CharacterId, amount, nameId, slot.ServerIndex, slot.Amount);

        // Wave 93 — rAthena pc_additem tail (pc.cpp:8268) calls
        // pc_updateweightstatus after every inventory mutation so the
        // SC_WEIGHT50 / SC_WEIGHT90 overlay tracks the new bag weight.
        if (_weightStatus != null && _entityRegistry != null
            && session.EntityId is { } eid
            && _entityRegistry.Get(eid) is PlayerEntity pc)
        {
            _weightStatus.UpdateWeightStatus(pc);
        }
        return true;
    }

    public bool GiveItemWithCards(MapSessionData session, uint nameId, int amount, uint card0, uint card1, uint card2, uint card3)
    {
        if (amount <= 0) return false;
        var items = session.Inventory;
        if (items is null)
        {
            _logger.LogWarning("GiveItemWithCards({NameId}) before LoadAsync ran — refusing", nameId);
            return false;
        }
        var entry = _itemCatalog.Get(nameId);
        if (entry == null)
        {
            _logger.LogWarning("GiveItemWithCards: unknown item id {NameId}", nameId);
            return false;
        }
        var type = ItemTypeCodes.FromDbString(entry.Type);

        // Card-bound specials never merge (rAthena itemdb_isspecial) — always a fresh slot.
        var slot = AppendNewSlot(items, nameId, type);
        slot.Amount = (uint)amount;
        slot.Card0 = card0;
        slot.Card1 = card1;
        slot.Card2 = card2;
        slot.Card3 = card3;

        session.EnqueuePacket(new ZC_ITEM_PICKUP_ACK
        {
            Index = (short)(slot.ServerIndex + 2),
            Count = (short)amount,
            NameId = nameId,
            IsIdentified = (byte)(slot.Identified ? 1 : 0),
            IsDamaged = 0,
            Card0 = slot.Card0,
            Card1 = slot.Card1,
            Card2 = slot.Card2,
            Card3 = slot.Card3,
            Location = 0,
            Type = type,
            Result = 0,
            HireExpireDate = (int)slot.ExpireTime,
            BindOnEquipType = 0,
            Favorite = slot.Favorite,
            Look = 0,
            RefiningLevel = slot.Refine,
            EnchantGrade = slot.EnchantGrade,
        });

        if (_weightStatus != null && _entityRegistry != null
            && session.EntityId is { } eid
            && _entityRegistry.Get(eid) is PlayerEntity pc)
        {
            _weightStatus.UpdateWeightStatus(pc);
        }
        return true;
    }

    private static bool StacksWith(InventoryItem slot, uint nameId)
    {
        // Naive stack key — same nameid + identified + no cards + no refine
        // + no random options. Matches the simple case where one identical
        // "fresh" potion / arrow stack should merge with another. Full
        // rAthena `pc_checkitem` stacking rules can land later.
        if (slot.NameId != nameId) return false;
        if (!slot.Identified) return false;
        if (slot.Refine != 0) return false;
        if (slot.Attribute != 0) return false;
        if (slot.Card0 != 0 || slot.Card1 != 0 || slot.Card2 != 0 || slot.Card3 != 0) return false;
        if (slot.Options.Any(o => o.Id != 0)) return false;
        return true;
    }

    private static InventoryItem AppendNewSlot(List<InventoryItem> items, uint nameId, byte _typeUnused)
    {
        var nextIndex = items.Count == 0 ? 0 : items.Max(i => i.ServerIndex) + 1;
        var slot = new InventoryItem
        {
            Id = 0,                // unsaved; PlayerStateService.SaveAsync assigns on insert (slice 3)
            ServerIndex = nextIndex,
            NameId = nameId,
            Amount = 0,            // caller sets
            Identified = true,
            Options = new ItemOption[5],
        };
        items.Add(slot);
        return slot;
    }
}
