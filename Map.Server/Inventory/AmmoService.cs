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
    /// COMBAT-58 — enough equipped ammo for <paramref name="qty"/> rounds (rAthena's
    /// <c>amount &lt; require.ammo_qty</c> gate for ammo-using skills). Default
    /// delegates to the 1-round check for test doubles; the real service checks the
    /// equipped amount against <paramref name="qty"/>.
    /// </summary>
    bool HasUsableAmmo(PlayerEntity pc, int qty) => HasUsableAmmo(pc);

    /// <summary>
    /// COMBAT-76 — enough equipped ammo for <paramref name="qty"/> rounds whose item
    /// subtype matches the skill's <paramref name="ammoMask"/> (rAthena <c>require.ammo</c>,
    /// a <c>1&lt;&lt;AMMO_*</c> bitmask). When <paramref name="ammoMask"/> is 0 the skill rides
    /// the weapon-ammo heuristic, so this delegates to the weapon-based check. The mask path
    /// is weapon-independent (a Kunai-throwing skill needs Kunai equipped, not a bow/gun).
    /// </summary>
    bool HasUsableAmmo(PlayerEntity pc, int qty, int ammoMask) => HasUsableAmmo(pc, qty);

    /// <summary>
    /// Spend one equipped round (rAthena <c>battle_consume_ammo</c>). No-op for
    /// non-ammo weapons. Removes the stack + clears the equip bit when it hits 0
    /// (rAthena <c>pc_delitem</c>); returns true when a round was consumed.
    /// </summary>
    bool ConsumeAmmo(PlayerEntity pc);

    /// <summary>
    /// COMBAT-76 — spend <paramref name="qty"/> rounds of the ammo matching
    /// <paramref name="ammoMask"/> (rAthena <c>battle_consume_ammo</c> on the skill's
    /// <c>require.ammo</c>). Mask 0 → the weapon-ammo path.
    /// </summary>
    bool ConsumeAmmo(PlayerEntity pc, int qty, int ammoMask) => ConsumeAmmo(pc, qty);

    /// <summary>
    /// COMBAT-58 — spend <paramref name="qty"/> rounds (rAthena
    /// <c>battle_consume_ammo</c> with <c>skill_get_ammo_qty</c>). Default loops the
    /// 1-round consume for test doubles; the real service deletes the stack in one go.
    /// </summary>
    bool ConsumeAmmo(PlayerEntity pc, int qty)
    {
        var ok = true;
        for (var i = 0; i < qty; i++) ok &= ConsumeAmmo(pc);
        return ok;
    }

    /// <summary>
    /// COMBAT-37 — the live <c>Amount</c> of the equipped (type-valid) ammo, or 0
    /// when none is equipped. Mirrors rAthena reading
    /// <c>sd-&gt;inventory.u.items_inventory[EQI_AMMO].amount</c> (the Fear Breeze
    /// div cap reads it each swing).
    /// </summary>
    int GetEquippedAmmoAmount(PlayerEntity pc);
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

    public bool HasUsableAmmo(PlayerEntity pc) => HasUsableAmmo(pc, 1);

    public bool HasUsableAmmo(PlayerEntity pc, int qty)
    {
        if (!WeaponTypeCodes.UsesAmmo(pc.WeaponType)) return true;
        var (ammo, slot) = FindEquippedAmmo(pc);
        return slot >= 0 && ammo!.Amount >= (uint)Math.Max(1, qty);
    }

    public bool HasUsableAmmo(PlayerEntity pc, int qty, int ammoMask)
    {
        // COMBAT-76 — explicit skill ammo mask: weapon-independent (a Kunai/Shuriken/
        // Cannonball skill gates on the matching equipped ammo regardless of weapon type).
        if (ammoMask == 0) return HasUsableAmmo(pc, qty);
        var (ammo, slot) = FindEquippedAmmoByMask(pc, ammoMask);
        return slot >= 0 && ammo!.Amount >= (uint)Math.Max(1, qty);
    }

    public bool ConsumeAmmo(PlayerEntity pc) => ConsumeAmmo(pc, 1);

    public bool ConsumeAmmo(PlayerEntity pc, int qty) => ConsumeAmmoFrom(pc, qty, FindEquippedAmmo(pc), weaponGated: true);

    public bool ConsumeAmmo(PlayerEntity pc, int qty, int ammoMask)
        => ammoMask == 0
            ? ConsumeAmmo(pc, qty)
            : ConsumeAmmoFrom(pc, qty, FindEquippedAmmoByMask(pc, ammoMask), weaponGated: false);

    private bool ConsumeAmmoFrom(PlayerEntity pc, int qty, (InventoryItem? ammo, int slot) found, bool weaponGated)
    {
        if (weaponGated && !WeaponTypeCodes.UsesAmmo(pc.WeaponType)) return true;
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Inventory is not { } inv) return false;
        var (ammo, slot) = found;
        if (ammo == null || slot < 0) return false;

        // rAthena battle_consume_ammo → pc_delitem(qty): spend `qty` rounds; drop the
        // stack + clear the equip bit when it empties. Removal rides the same
        // RemovedInventoryIds client-sync path as ItemUseService.
        var take = (uint)Math.Min(ammo.Amount, Math.Max(1, qty));
        ammo.Amount -= take;
        if (ammo.Amount == 0)
        {
            ammo.Equip = 0;
            if (ammo.Id > 0) session.RemovedInventoryIds.Add(ammo.Id);
            inv.RemoveAt(slot);
        }
        else
        {
            // COMBAT-94 — partial decrement: tell the client the new count immediately
            // (rAthena battle_consume_ammo → pc_delitem reason 1 = "used for a skill"). A full
            // removal (amount == 0) still rides the RemovedInventoryIds sync above.
            session.NotifyItemConsumed(ammo.ServerIndex, take, reason: 1);
        }
        return true;
    }

    public int GetEquippedAmmoAmount(PlayerEntity pc)
        => (int)(FindEquippedAmmo(pc).ammo?.Amount ?? 0);

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
    /// COMBAT-76 — locate the equipped ammo whose item subtype bit is set in
    /// <paramref name="ammoMask"/> (rAthena <c>require.ammo &amp; 1&lt;&lt;subtype</c>,
    /// skill.cpp:19633). Weapon-independent — used for explicit-ammo skills (Kunai /
    /// Shuriken / Cannonball / Throw) whose weapon isn't a bow/gun.
    /// </summary>
    private (InventoryItem? ammo, int slot) FindEquippedAmmoByMask(PlayerEntity pc, int ammoMask)
    {
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.Inventory is not { } inv) return (null, -1);
        for (var i = 0; i < inv.Count; i++)
        {
            var item = inv[i];
            if (item.Amount == 0) continue;
            if ((item.Equip & EquipBonusAggregator.EquipAmmo) == 0) continue;
            var bit = AmmoSubtypeBit(_catalog.Get(item.NameId)?.Subtype);
            if (bit != 0 && (bit & ammoMask) != 0) return (item, i);
        }
        return (null, -1);
    }

    /// <summary>rAthena <c>e_ammo_type</c> (pc.hpp:1001): item subtype → <c>1&lt;&lt;AMMO_*</c> bit.</summary>
    private static int AmmoSubtypeBit(string? subtype) => subtype switch
    {
        "Arrow" => 1 << 1, "Dagger" => 1 << 2, "Bullet" => 1 << 3, "Shell" => 1 << 4,
        "Grenade" => 1 << 5, "Shuriken" => 1 << 6, "Kunai" => 1 << 7,
        "Cannonball" => 1 << 8, "Throwweapon" => 1 << 9,
        _ => 0,
    };

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
