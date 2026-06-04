using Map.Server.Inventory;

namespace Map.Server.Pet;

/// <summary>
/// Binds a persistent pet_id into a pet-egg item's card slots, mirroring rAthena's
/// <c>CARD0_PET</c> convention (itemdb.hpp): <c>card[0] = CARD0_PET</c> marks the egg as carrying a
/// saved pet, and <c>card[1]/card[2]</c> hold the low/high 16-bit words of the pet_id. This is how a
/// hatched pet's intimacy/hunger/name survive being packed back into an egg and re-hatched.
/// </summary>
public static class PetEggCard
{
    /// <summary>rAthena <c>CARD0_PET</c> (itemdb.hpp) — the card[0] marker for a bound pet egg.</summary>
    public const uint Card0Pet = 0x0100;

    /// <summary>Split a pet_id into the (card0, card1, card2) the egg should carry.</summary>
    public static (uint Card0, uint Card1, uint Card2) Bind(int petId)
        => (Card0Pet, (uint)(petId & 0xFFFF), (uint)((petId >> 16) & 0xFFFF));

    /// <summary>Read the bound pet_id from an egg item, or null if it carries no pet binding.</summary>
    public static int? ReadPetId(InventoryItem item)
    {
        if (item.Card0 != Card0Pet) return null;
        return (int)((item.Card1 & 0xFFFF) | ((item.Card2 & 0xFFFF) << 16));
    }
}
