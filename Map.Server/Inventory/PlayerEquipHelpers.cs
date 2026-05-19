using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Inventory;

/// <summary>
/// Default <see cref="IPlayerEquipHelpers"/>. The big-feature
/// behaviors (insert_card, costume layering, rental sweep) are
/// stubbed with a documented no-op so the API surface is canonical
/// but the rAthena 1:1 work lives in the sub-system docs (see
/// pc-parity.md "Equipment" + "Inventory" sections).
/// </summary>
public sealed class PlayerEquipHelpers : IPlayerEquipHelpers
{
    private readonly IPlayerLookService _look;
    private readonly Status.ISessionManagerAccessor _sessions;
    private readonly ILogger<PlayerEquipHelpers> _logger;

    public PlayerEquipHelpers(
        IPlayerLookService look,
        Status.ISessionManagerAccessor sessions,
        ILogger<PlayerEquipHelpers> logger)
    {
        _look = look;
        _sessions = sessions;
        _logger = logger;
    }

    public void CalcWeaponType(PlayerEntity pc)
    {
        // First slice: weapon type already flows through EquipBonusAggregator
        // when the right-hand slot mutates. Dual-wield composite stays
        // deferred — battle.cpp doesn't honor it yet either in our port.
    }

    public void EquipLookAll(PlayerEntity pc)
    {
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.CharacterData == null) return;
        // rAthena re-emits clif_changelook for each of LOOK_HEAD_TOP,
        // LOOK_HEAD_MID, LOOK_HEAD_BOTTOM, LOOK_WEAPON, LOOK_SHIELD,
        // LOOK_SHOES, LOOK_ROBE. We mirror via existing IPlayerLookService.
        var d = session.CharacterData;
        _look.ChangeLook(pc, LookType.HeadTop, (ushort)d.Weapon);  // proto field name doesn't matter here, placeholder
        _look.ChangeLook(pc, LookType.Weapon, (ushort)d.Weapon);
        _look.ChangeLook(pc, LookType.Shield, (ushort)d.Shield);
        // Hair/Clothes color already broadcast on standentry; only the
        // gear-mutating slots replay here.
    }

    public void EquipSwitchRemove(PlayerEntity pc, int inventoryIndex)
    {
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Inventory == null
            || inventoryIndex < 0 || inventoryIndex >= session.Inventory.Count) return;
        var row = session.Inventory[inventoryIndex];
        row.EquipSwitch = 0;
    }

    public void SetCostumeView(PlayerEntity pc)
    {
        // No-op until costume stat-suppression lands.
    }

    public bool InsertCard(PlayerEntity pc, int cardInventoryIndex, int equipInventoryIndex)
    {
        // Stub — full slotting needs the card-slot writer + slot
        // count validation. Logged so QA spots the call.
        _logger.LogDebug(
            "pc_insert_card stub: char {Char} card@{Card} → equip@{Equip}",
            pc.CharacterId, cardInventoryIndex, equipInventoryIndex);
        return false;
    }

    public void CheckExpiration(PlayerEntity pc)
    {
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Inventory == null) return;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var removed = 0;
        for (var i = session.Inventory.Count - 1; i >= 0; i--)
        {
            var row = session.Inventory[i];
            if (row.ExpireTime != 0 && row.ExpireTime < now)
            {
                if (row.Id > 0) session.RemovedInventoryIds.Add(row.Id);
                session.Inventory.RemoveAt(i);
                removed++;
            }
        }
        if (removed > 0)
        {
            _logger.LogInformation(
                "pc_check_expiration: char {Char} dropped {Count} expired item(s)",
                pc.CharacterId, removed);
        }
    }
}
