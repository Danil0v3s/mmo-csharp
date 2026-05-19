namespace Map.Server.Status;

/// <summary>
/// Mirror of rAthena <c>enum _look</c> (map.hpp:585). Drives the
/// per-slot appearance broadcast in <c>clif_changelook</c> /
/// <c>ZC_SPRITE_CHANGE2</c>.
///
/// Values are pinned to the rAthena indices so the wire and any
/// imported character data line up without remapping.
/// </summary>
public enum LookType : byte
{
    Base = 0,
    Hair = 1,
    Weapon = 2,
    HeadBottom = 3,
    HeadTop = 4,
    HeadMid = 5,
    HairColor = 6,
    ClothesColor = 7,
    Shield = 8,
    Shoes = 9,
    Body = 10,           // unused in client; reserved
    ResetCostumes = 11,  // sentinel that vanishes every headgear sprite
    Robe = 12,
    Body2 = 13,
}
