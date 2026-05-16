namespace Map.Server.Entities;

/// <summary>
/// Bit-flag entity types matching rAthena's <c>enum bl_type</c> in map.hpp.
/// Used as a filter mask for <c>ForEachInRange(..., mask)</c> queries.
/// </summary>
[Flags]
public enum EntityType : ushort
{
    None = 0,
    Pc = 0x001,    // Player character
    Mob = 0x002,
    Pet = 0x004,
    Homunculus = 0x008,
    Mercenary = 0x010,
    Elemental = 0x020,
    Npc = 0x040,
    Item = 0x080,  // Floor item
    Skill = 0x100, // Skill unit (Storm Gust, Ice Wall, etc.)
    Chat = 0x200,  // Chat room
    All = 0xFFFF,
}
