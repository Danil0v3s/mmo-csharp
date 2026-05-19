using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;
using Microsoft.Extensions.Logging;

namespace Map.Server.Items;

public sealed class ItemDropService : IItemDropService
{
    private readonly IEntityRegistry _entities;
    private readonly EntityIdAllocator _ids;
    private readonly IVisibilityService _visibility;
    private readonly ILogger<ItemDropService> _logger;
    private readonly int _lifetimeMs;

    public ItemDropService(
        IEntityRegistry entities,
        EntityIdAllocator ids,
        IVisibilityService visibility,
        ILogger<ItemDropService> logger,
        int lifetimeMs = IItemDropService.DefaultLifetimeMs)
    {
        _entities = entities;
        _ids = ids;
        _visibility = visibility;
        _logger = logger;
        _lifetimeMs = lifetimeMs;
    }

    public EntityId DropOnFloor(
        uint mapId, short x, short y, int itemId, short amount,
        byte subX = 0, byte subY = 0, bool identified = true,
        int ownerCharId = 0, int ownerPartyId = 0,
        int ownerGuildId = 0, bool isMvpDrop = false)
    {
        var now = Environment.TickCount64;
        // rAthena tiers are cumulative — each "until" tick stacks on the
        // previous one. MVP-class drops swap in the longer mvp_item_*
        // values from battle.cpp:11508.
        var ownerMs = isMvpDrop ? IItemDropService.MvpOwnerProtectionMs : IItemDropService.OwnerProtectionMs;
        var partyMs = isMvpDrop ? IItemDropService.MvpPartyProtectionMs : IItemDropService.PartyProtectionMs;
        var guildMs = isMvpDrop ? IItemDropService.MvpGuildProtectionMs : IItemDropService.GuildProtectionMs;
        var ownerUntil = ownerCharId > 0 ? now + ownerMs : 0;
        var partyUntil = ownerPartyId > 0 ? ownerUntil + partyMs : ownerUntil;
        var guildUntil = ownerGuildId > 0 ? partyUntil + guildMs : partyUntil;

        var item = new FloorItemEntity(
            _ids.NextItem(),
            itemId, amount,
            mapId, x, y,
            subX, subY,
            droppedAtTick: now,
            identified: (byte)(identified ? 1 : 0))
        {
            OwnerCharId = ownerCharId,
            OwnerPartyId = ownerPartyId,
            OwnerGuildId = ownerGuildId,
            IsMvpDrop = isMvpDrop,
            OwnerProtectionUntilTick = ownerUntil,
            PartyProtectionUntilTick = partyUntil,
            GuildProtectionUntilTick = guildUntil,
        };
        _entities.Add(item);

        // Drop animation: broadcast ZC_ITEM_FALL_ENTRY to PC viewers in AOI.
        _visibility.SendToArea(item, new ZC_ITEM_FALL_ENTRY
        {
            EntityId = item.Id.Value,
            ItemId = item.ItemId,
            ItemType = 0, // populated when item_db lands; rAthena calls itemtype()
            Identified = item.Identified,
            X = item.X, Y = item.Y,
            SubX = item.SubX, SubY = item.SubY,
            Amount = item.Amount,
        });
        return item.Id;
    }

    public IItemDropService.PickupResult TryPickup(
        PlayerEntity picker, EntityId itemEntityId, out FloorItemEntity? item)
    {
        item = _entities.Get(itemEntityId) as FloorItemEntity;
        if (item == null)
        {
            return IItemDropService.PickupResult.ItemNotFound;
        }
        if (item.MapId != picker.MapId)
        {
            return IItemDropService.PickupResult.WrongMap;
        }
        if (Math.Abs(picker.X - item.X) > IItemDropService.PickupRange
            || Math.Abs(picker.Y - item.Y) > IItemDropService.PickupRange)
        {
            return IItemDropService.PickupResult.OutOfRange;
        }

        // Loot-protection windows — rAthena mob_dead's first/second/
        // third_charid logic collapsed into cumulative time gates.
        // Inside owner-only window: only the owner gets it.
        // Past owner, inside party-only: owner OR same-party get it.
        // Past party, inside guild-only: owner OR same-party OR
        // same-guild get it. After all three, the drop is public.
        var now = Environment.TickCount64;
        if (item.OwnerCharId > 0
            && now < item.OwnerProtectionUntilTick
            && picker.CharacterId != item.OwnerCharId)
        {
            return IItemDropService.PickupResult.OwnerProtected;
        }
        if (item.OwnerPartyId > 0
            && now < item.PartyProtectionUntilTick
            && picker.CharacterId != item.OwnerCharId
            && picker.PartyId != item.OwnerPartyId)
        {
            return IItemDropService.PickupResult.OwnerProtected;
        }
        if (item.OwnerGuildId > 0
            && now < item.GuildProtectionUntilTick
            && picker.CharacterId != item.OwnerCharId
            && picker.PartyId != item.OwnerPartyId
            && picker.GuildId != item.OwnerGuildId)
        {
            return IItemDropService.PickupResult.OwnerProtected;
        }

        // Removing first prevents a race where the broadcast scan re-finds
        // the item in AOI; the visibility service's SendToArea iterates the
        // spatial index which still holds the just-removed entity.
        _visibility.NotifyVanishedToArea(item, VanishReason.Outsight);
        _entities.Remove(itemEntityId);
        return IItemDropService.PickupResult.Ok;
    }

    public void Tick()
    {
        var now = Environment.TickCount64;
        List<FloorItemEntity>? expired = null;
        foreach (var entity in _entities.All())
        {
            if (entity is not FloorItemEntity item) continue;
            if (now - item.DroppedAtTick < _lifetimeMs) continue;
            (expired ??= new List<FloorItemEntity>()).Add(item);
        }
        if (expired == null) return;

        foreach (var item in expired)
        {
            _visibility.NotifyVanishedToArea(item, VanishReason.Outsight);
            _entities.Remove(item.Id);
        }
        _logger.LogDebug("Despawned {Count} floor items", expired.Count);
    }
}
