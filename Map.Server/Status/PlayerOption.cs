namespace Map.Server.Status;

/// <summary>
/// Mirror of rAthena <c>enum e_option</c> (status.hpp:2980) — the
/// 32-bit effect-state bitmask the wire ships in
/// <c>ZC_STATE_CHANGE / ZC_STATE_CHANGE3</c>. Values are pinned so the
/// client renders the right sprite without a remap.
///
/// <para>Compound aliases (OPTION_CART, OPTION_DRAGON, OPTION_COSTUME)
/// live as helper properties on <see cref="PlayerOptionExtensions"/>.</para>
/// </summary>
[Flags]
public enum PlayerOption : uint
{
    Nothing        = 0x0,
    Sight          = 0x00000001,
    Hide           = 0x00000002,
    Cloak          = 0x00000004,
    Cart1          = 0x00000008,
    Falcon         = 0x00000010,
    Riding         = 0x00000020,
    Invisible      = 0x00000040,
    Cart2          = 0x00000080,
    Cart3          = 0x00000100,
    Cart4          = 0x00000200,
    Cart5          = 0x00000400,
    Orcish         = 0x00000800,
    Wedding        = 0x00001000,
    Ruwach         = 0x00002000,
    Chasewalk      = 0x00004000,
    Flying         = 0x00008000,
    Xmas           = 0x00010000,
    Transform      = 0x00020000,
    Summer         = 0x00040000,
    Dragon1        = 0x00080000,
    Wug            = 0x00100000,
    Wugrider       = 0x00200000,
    Madogear       = 0x00400000,
    Dragon2        = 0x00800000,
    Dragon3        = 0x01000000,
    Dragon4        = 0x02000000,
    Dragon5        = 0x04000000,
    Hanbok         = 0x08000000,
    Oktoberfest    = 0x10000000,
    Summer2        = 0x20000000,
}

public static class PlayerOptionExtensions
{
    /// <summary>Any of the 5 cart sprites.</summary>
    public const PlayerOption AnyCart = PlayerOption.Cart1 | PlayerOption.Cart2 | PlayerOption.Cart3 | PlayerOption.Cart4 | PlayerOption.Cart5;
    /// <summary>Any of the 5 dragon sprites.</summary>
    public const PlayerOption AnyDragon = PlayerOption.Dragon1 | PlayerOption.Dragon2 | PlayerOption.Dragon3 | PlayerOption.Dragon4 | PlayerOption.Dragon5;
    /// <summary>Wedding + Xmas + Summer + Hanbok + Oktoberfest + Summer2.</summary>
    public const PlayerOption AnyCostume = PlayerOption.Wedding | PlayerOption.Xmas | PlayerOption.Summer | PlayerOption.Hanbok | PlayerOption.Oktoberfest | PlayerOption.Summer2;

    public static bool HasCart(this PlayerOption opt) => (opt & AnyCart) != 0;
    public static bool HasDragon(this PlayerOption opt) => (opt & AnyDragon) != 0;
    public static bool HasCostume(this PlayerOption opt) => (opt & AnyCostume) != 0;
}
