namespace Map.Server.Inventory;

/// <summary>
/// rAthena <c>IT_*</c> enum values, used on the wire as the <c>type</c>
/// byte in NORMALITEM_INFO / EQUIPITEM_INFO. Mirrors
/// <c>src/common/mmo.hpp</c> in rAthena.
/// </summary>
public static class ItemTypeCodes
{
    public const byte Healing       = 0;
    public const byte Usable        = 2;
    public const byte Etc           = 3;
    public const byte Weapon        = 4;
    public const byte Armor         = 5;
    public const byte Card          = 6;
    public const byte PetEgg        = 7;
    public const byte PetArmor      = 8;
    public const byte Ammo          = 10;
    public const byte DelayConsume  = 11;
    public const byte Shadowgear    = 12;
    public const byte Cash          = 18;

    /// <summary>Map the DB <c>type</c> column string to the wire byte.</summary>
    public static byte FromDbString(string? type) => type switch
    {
        "Healing"       => Healing,
        "Usable"        => Usable,
        "Etc"           => Etc,
        "Weapon"        => Weapon,
        "Armor"         => Armor,
        "Card"          => Card,
        "Petegg"        => PetEgg,
        "Petarmor"      => PetArmor,
        "Ammo"          => Ammo,
        "DelayConsume"  => DelayConsume,
        "Shadowgear"    => Shadowgear,
        "Cash"          => Cash,
        _               => Etc,
    };

    /// <summary>
    /// Whether items of this type go in the EQUIP list rather than the
    /// NORMAL list during <c>clif_inventorylist</c>. Mirrors
    /// <c>itemdb_isstackable2</c> in rAthena <c>itemdb.cpp</c>.
    /// </summary>
    public static bool IsEquippable(byte typeCode) =>
        typeCode is Weapon or Armor or PetArmor or Shadowgear;
}
