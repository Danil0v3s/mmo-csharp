using System;
using System.Collections.Generic;

namespace Map.Server.Inventory;

/// <summary>
/// COMBAT-16 — maps the <c>item_db</c> weapon <c>SubType</c> NAME
/// (e.g. "Knuckle", "Bow", "1hSword") to the rAthena <c>weapon_type</c>
/// enum value (<c>W_*</c>, pc.hpp). The DB stores the subtype as a string,
/// so the prior <c>int.TryParse(def.Subtype)</c> in <c>CalcWeaponType</c>
/// always failed → every equipped weapon resolved to 0 (W_FIST). This is the
/// canonical name→code table used by the equip aggregator + weapon-type calc.
/// </summary>
public static class WeaponTypeCodes
{
    // rAthena weapon_type (pc.hpp): bare hand = 0 (no item).
    public const int Fist = 0;
    public const int Dagger = 1;
    public const int OneHandSword = 2;
    public const int TwoHandSword = 3;
    public const int OneHandSpear = 4;
    public const int TwoHandSpear = 5;
    public const int OneHandAxe = 6;
    public const int TwoHandAxe = 7;
    public const int Mace = 8;
    public const int TwoHandMace = 9;
    public const int Staff = 10;
    public const int Bow = 11;
    public const int Knuckle = 12;
    public const int Musical = 13;
    public const int Whip = 14;
    public const int Book = 15;
    public const int Katar = 16;
    public const int Revolver = 17;
    public const int Rifle = 18;
    public const int Gatling = 19;
    public const int Shotgun = 20;
    public const int Grenade = 21;
    public const int Huuma = 22;
    public const int TwoHandStaff = 23;

    private static readonly Dictionary<string, int> _byName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Fist"] = Fist,
        ["Dagger"] = Dagger,
        ["1hSword"] = OneHandSword,
        ["2hSword"] = TwoHandSword,
        ["1hSpear"] = OneHandSpear,
        ["2hSpear"] = TwoHandSpear,
        ["1hAxe"] = OneHandAxe,
        ["2hAxe"] = TwoHandAxe,
        ["Mace"] = Mace,
        ["2hMace"] = TwoHandMace,
        ["Staff"] = Staff,
        ["Bow"] = Bow,
        ["Knuckle"] = Knuckle,
        ["Musical"] = Musical,
        ["Whip"] = Whip,
        ["Book"] = Book,
        ["Katar"] = Katar,
        ["Revolver"] = Revolver,
        ["Rifle"] = Rifle,
        ["Gatling"] = Gatling,
        ["Shotgun"] = Shotgun,
        ["Grenade"] = Grenade,
        ["Huuma"] = Huuma,
        ["2hStaff"] = TwoHandStaff,
    };

    /// <summary>
    /// Resolve a weapon <c>SubType</c> to its <c>W_*</c> code. Accepts the
    /// rAthena name (preferred) or a numeric string (legacy). Unknown / null
    /// → <see cref="Fist"/> (0).
    /// </summary>
    public static int FromSubtype(string? subtype)
    {
        if (string.IsNullOrEmpty(subtype)) return Fist;
        if (_byName.TryGetValue(subtype, out var code)) return code;
        return int.TryParse(subtype, out var n) ? n : Fist;
    }

    /// <summary>
    /// Renewal <c>size_fix.yml</c>: only Knuckle and Whip take a non-100
    /// size penalty (75% vs Large). Every other weapon/size is 100%.
    /// </summary>
    public static bool IsKnuckleOrWhip(int weaponType) => weaponType is Knuckle or Whip;

    /// <summary>
    /// Ammo-consuming ranged weapons (rAthena: bow + the gunslinger guns).
    /// Their attacks add the equipped ammo's ATK (<c>arrow_atk</c>) and
    /// consume one round per shot. Musical/Whip are "ranged" for ASPD but do
    /// not use ammo.
    /// </summary>
    public static bool UsesAmmo(int weaponType)
        => weaponType is Bow or Revolver or Rifle or Gatling or Shotgun or Grenade;
}
