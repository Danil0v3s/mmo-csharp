using Map.Server.Mob;

namespace Map.Server.Entities;

/// <summary>
/// A live pet on the map. Specialised <see cref="MobEntity"/> that
/// carries owner-data + intimacy + hunger. Mirrors rAthena
/// <c>struct pet_data</c> (pet.hpp).
///
/// AI behavior (follow + assist) flows through the generic
/// <see cref="Mob.ISummonAiService"/> via <see cref="Entity.MasterId"/>;
/// per-pet specifics (intimacy on heal, hunger decay, autofeed)
/// live in <see cref="Pet.IPetService"/>.
/// </summary>
public sealed class PetEntity : MobEntity
{
    /// <summary>Persistent pet id from char-server (pet table primary key).</summary>
    public long PetId { get; init; }

    /// <summary>
    /// Item id of the egg this pet hatched from. Mirrors rAthena
    /// <c>s_pet.egg_id</c> (pet.hpp), exposed to scripts via
    /// <c>getpetinfo(PETINFO_EGGID)</c>. Item-script conditionals like the
    /// Wonder Egg Basket family branch on this value to grant per-egg
    /// bonuses, so it has to be available at recalc time.
    /// </summary>
    public int EggId { get; init; }

    /// <summary>Intimacy (0..1000). Pet befriends / dislikes the player as this moves.</summary>
    public ushort Intimacy { get; set; } = 250; // rAthena PETINTIMATE_NEUTRAL

    /// <summary>Hunger (0..100). Decays over time; hits 0 = pet runs away.</summary>
    public ushort Hunger { get; set; } = 80; // rAthena PETHUNGER_SATISFIED

    /// <summary>Equipped item id (pet accessory). 0 = none.</summary>
    public uint EquipItemId { get; set; }

    /// <summary>
    /// Pet auto-loot capacity — max items the pet collects from kills
    /// before depositing back to the owner. Mirrors rAthena
    /// <c>pet_db.AutoFeed</c> + <c>petloot N</c> script function.
    /// Set by item scripts via <c>petloot N</c> on equip; the pet AI
    /// step reads this when deciding whether to pick up a floor item.
    /// 0 = no auto-loot (default).
    /// </summary>
    public int AutoLootMax { get; set; }

    /// <summary>Display name override; falls back to mob_db Name + a "'s pet" suffix in clients.</summary>
    public string PetName { get; set; } = string.Empty;

    /// <summary>True once the pet has been renamed (rAthena <c>s_pet.rename_flag</c>): a renamed pet
    /// can't be renamed again unless <c>battle_config.pet_rename</c> is on. Drives the
    /// <c>ZC_PROPERTY_PET</c> "modified" byte.</summary>
    public bool RenameFlag { get; set; }

    public PetEntity(EntityId id, MobDbEntry dbEntry, uint mapId, short x, short y)
        : base(id, dbEntry.Id, dbEntry.AegisName, mapId, x, y)
    {
        // Inherited MobEntity constructor takes ClassId + Name; rest of
        // the db hydration runs through IStatusCalcService.CalcMob.
    }
}
