using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Session;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Inventory;

/// <summary>
/// Default <see cref="IPlayerEquipHelpers"/>. Equipment-side rAthena
/// helpers: weapon-type compositing, slot-by-slot appearance replay,
/// equipswitch clearing, card insertion, expiration sweep.
/// </summary>
public sealed class PlayerEquipHelpers : IPlayerEquipHelpers
{
    private readonly IPlayerLookService _look;
    private readonly Status.ISessionManagerAccessor _sessions;
    private readonly IItemCatalog _catalog;
    private readonly ILogger<PlayerEquipHelpers> _logger;

    public PlayerEquipHelpers(
        IPlayerLookService look,
        Status.ISessionManagerAccessor sessions,
        IItemCatalog catalog,
        ILogger<PlayerEquipHelpers> logger)
    {
        _look = look;
        _sessions = sessions;
        _catalog = catalog;
        _logger = logger;
    }

    public void CalcWeaponType(PlayerEntity pc)
    {
        // rAthena pc_calcweapontype (pc.cpp:923): derives sd->weapontype
        // from the equipped right-hand + left-hand weapon types.
        // Composite "dual" detection is class-mask gated; first slice
        // copies the right-hand's subtype int (0 = bare).
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Inventory == null) { pc.WeaponType = 0; return; }
        int rhSubtype = 0;
        int lhSubtype = 0;
        foreach (var row in session.Inventory)
        {
            if (row.Equip == 0) continue;
            var def = _catalog.Get(row.NameId);
            if (def == null) continue;
            // COMBAT-16: item_db SubType is a NAME ("Knuckle"/"Bow"/…), not an
            // int — resolve via WeaponTypeCodes (the old int.TryParse silently
            // left every weapon at 0/Fist).
            if ((row.Equip & EquipBits.HandR) != 0) rhSubtype = WeaponTypeCodes.FromSubtype(def.Subtype);
            if ((row.Equip & EquipBits.HandL) != 0) lhSubtype = WeaponTypeCodes.FromSubtype(def.Subtype);
        }
        // Store on PlayerEntity for skill plugins that branch on weapon kind.
        // Mirrors rAthena `sd->status.weapon` (the persistable right-hand)
        // and `sd->weapontype` (the composite). First-slice records the
        // right-hand subtype only; dual-wield W_DOUBLE_* composite when
        // the class-mask dispatcher ports.
        pc.WeaponType = rhSubtype;
        _logger.LogDebug(
            "pc_calcweapontype: char {Char} R={Rh} L={Lh}",
            pc.CharacterId, rhSubtype, lhSubtype);
    }

    public void EquipLookAll(PlayerEntity pc)
    {
        // rAthena pc_equiplookall (pc.cpp:6203) — re-broadcasts every
        // visual equipped slot via clif_changelook.
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Inventory == null) return;
        foreach (var row in session.Inventory)
        {
            if (row.Equip == 0) continue;
            if ((row.Equip & EquipBits.HeadTop) != 0) _look.ChangeLook(pc, LookType.HeadTop, (ushort)row.NameId);
            if ((row.Equip & EquipBits.HeadMid) != 0) _look.ChangeLook(pc, LookType.HeadMid, (ushort)row.NameId);
            if ((row.Equip & EquipBits.HeadLow) != 0) _look.ChangeLook(pc, LookType.HeadBottom, (ushort)row.NameId);
            if ((row.Equip & EquipBits.HandR) != 0) _look.ChangeLook(pc, LookType.Weapon, (ushort)row.NameId);
            if ((row.Equip & EquipBits.HandL) != 0) _look.ChangeLook(pc, LookType.Shield, (ushort)row.NameId);
            if ((row.Equip & EquipBits.Shoes) != 0) _look.ChangeLook(pc, LookType.Shoes, (ushort)row.NameId);
            if ((row.Equip & EquipBits.Garment) != 0) _look.ChangeLook(pc, LookType.Robe, (ushort)row.NameId);
        }
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
        // rAthena pc_set_costume_view (pc.cpp:11796): a costume bypasses
        // item stats but renders its sprite over the regular gear slot.
        // We re-broadcast the costume-slot LookTypes; stat suppression
        // already happens in EquipBonusAggregator by skipping Costume*
        // bits.
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Inventory == null) return;
        foreach (var row in session.Inventory)
        {
            if (row.Equip == 0) continue;
            if ((row.Equip & EquipBits.CostumeHeadTop) != 0) _look.ChangeLook(pc, LookType.HeadTop, (ushort)row.NameId);
            if ((row.Equip & EquipBits.CostumeHeadMid) != 0) _look.ChangeLook(pc, LookType.HeadMid, (ushort)row.NameId);
            if ((row.Equip & EquipBits.CostumeHeadLow) != 0) _look.ChangeLook(pc, LookType.HeadBottom, (ushort)row.NameId);
            if ((row.Equip & EquipBits.CostumeGarment) != 0) _look.ChangeLook(pc, LookType.Robe, (ushort)row.NameId);
        }
    }

    public bool InsertCard(PlayerEntity pc, int cardInventoryIndex, int equipInventoryIndex)
    {
        // rAthena pc_insert_card (pc.cpp:5151): consume a card from
        // inventory, slot it into the first empty Card0..Card3 of the
        // target. Type-compat (armor card on armor, weapon card on
        // weapon) defers to the caller until the card_db mapping ports.
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Inventory == null) return false;
        if (cardInventoryIndex < 0 || cardInventoryIndex >= session.Inventory.Count) return false;
        if (equipInventoryIndex < 0 || equipInventoryIndex >= session.Inventory.Count) return false;
        if (cardInventoryIndex == equipInventoryIndex) return false;

        var card = session.Inventory[cardInventoryIndex];
        var equip = session.Inventory[equipInventoryIndex];
        var cardDef = _catalog.Get(card.NameId);
        var equipDef = _catalog.Get(equip.NameId);
        if (cardDef == null || equipDef == null) return false;

        if (equip.Card0 == 0) equip.Card0 = card.NameId;
        else if (equip.Card1 == 0) equip.Card1 = card.NameId;
        else if (equip.Card2 == 0) equip.Card2 = card.NameId;
        else if (equip.Card3 == 0) equip.Card3 = card.NameId;
        else return false;

        card.Amount -= 1;
        if (card.Amount == 0)
        {
            if (card.Id > 0) session.RemovedInventoryIds.Add(card.Id);
            session.Inventory.RemoveAt(cardInventoryIndex);
        }
        _logger.LogInformation(
            "pc_insert_card: char {Char} slotted card {Card} into item {Equip}",
            pc.CharacterId, card.NameId, equip.NameId);
        return true;
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
