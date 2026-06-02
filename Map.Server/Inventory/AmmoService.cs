using System;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Inventory;

/// <summary>
/// COMBAT-36 — ammo gate + consumption for ranged auto-attacks. rAthena
/// <c>battle_weapon_attack</c> refuses the swing (returns <c>ATK_NONE</c>) when a
/// bow/gun has no valid equipped ammo (battle.cpp:10386), and
/// <c>battle_consume_ammo</c> (battle.cpp:2580) spends one round per swing.
/// </summary>
public interface IAmmoService
{
    /// <summary>
    /// True if <paramref name="pc"/> may swing: the weapon does not use ammo, OR
    /// it uses ammo and a matching-type round is equipped with <c>Amount &gt; 0</c>.
    /// Mirrors rAthena's ammo gate (no ammo / wrong ammo type → cannot attack).
    /// </summary>
    bool HasUsableAmmo(PlayerEntity pc);

    /// <summary>
    /// Spend one equipped round (rAthena <c>battle_consume_ammo</c>). No-op for
    /// non-ammo weapons. Removes the stack + clears the equip bit when it hits 0
    /// (rAthena <c>pc_delitem</c>); returns true when a round was consumed.
    /// </summary>
    bool ConsumeAmmo(PlayerEntity pc);
}

/// <inheritdoc cref="IAmmoService"/>
public sealed class AmmoService : IAmmoService
{
    private readonly ISessionManagerAccessor _sessions;
    private readonly IItemCatalog _catalog;
    private readonly ILogger<AmmoService> _logger;

    public AmmoService(ISessionManagerAccessor sessions, IItemCatalog catalog, ILogger<AmmoService> logger)
    {
        _sessions = sessions;
        _catalog = catalog;
        _logger = logger;
    }

    public bool HasUsableAmmo(PlayerEntity pc)
    {
        if (!WeaponTypeCodes.UsesAmmo(pc.WeaponType)) return true;
        return FindEquippedAmmo(pc).slot >= 0;
    }

    public bool ConsumeAmmo(PlayerEntity pc)
    {
        if (!WeaponTypeCodes.UsesAmmo(pc.WeaponType)) return true;
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Inventory is not { } inv) return false;
        var (ammo, slot) = FindEquippedAmmo(pc);
        if (ammo == null || slot < 0) return false;

        // rAthena pc_delitem: one round per swing; drop the stack + clear the
        // equip bit when it empties. Removal rides the same RemovedInventoryIds
        // client-sync path as ItemUseService (no separate body packet exists).
        ammo.Amount -= 1;
        if (ammo.Amount == 0)
        {
            ammo.Equip = 0;
            if (ammo.Id > 0) session.RemovedInventoryIds.Add(ammo.Id);
            inv.RemoveAt(slot);
        }
        return true;
    }

    /// <summary>
    /// Locate the equipped ammo whose subtype matches the weapon (Arrow↔Bow,
    /// Bullet↔gun). A mismatch / empty stack is treated as "no ammo" (rAthena's
    /// per-weapon subtype switch → <c>ATK_NONE</c>).
    /// </summary>
    private (InventoryItem? ammo, int slot) FindEquippedAmmo(PlayerEntity pc)
    {
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Inventory is not { } inv) return (null, -1);
        var required = RequiredAmmoSubtype(pc.WeaponType);
        for (var i = 0; i < inv.Count; i++)
        {
            var item = inv[i];
            if (item.Amount == 0) continue;
            if ((item.Equip & EquipBonusAggregator.EquipAmmo) == 0) continue;
            var subtype = _catalog.Get(item.NameId)?.Subtype;
            if (required != null && !string.Equals(subtype, required, StringComparison.OrdinalIgnoreCase))
                continue; // wrong ammo type → not usable for this weapon
            return (item, i);
        }
        return (null, -1);
    }

    /// <summary>
    /// rAthena ammo-type gate (battle.cpp:10401-10426, RENEWAL): a bow needs an
    /// <c>AMMO_ARROW</c> (item subtype "Arrow"); every gun (Revolver..Grenade)
    /// needs an <c>AMMO_BULLET</c> ("Bullet"). Returns null when the weapon does
    /// not constrain ammo type.
    /// </summary>
    private static string? RequiredAmmoSubtype(int weaponType) => weaponType switch
    {
        WeaponTypeCodes.Bow => "Arrow",
        WeaponTypeCodes.Revolver or WeaponTypeCodes.Rifle or WeaponTypeCodes.Gatling
            or WeaponTypeCodes.Shotgun or WeaponTypeCodes.Grenade => "Bullet",
        _ => null,
    };
}
