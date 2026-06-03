using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Status;
using Map.Server.Visibility;
using Microsoft.Extensions.Logging;

namespace Map.Server.Pet;

/// <summary>
/// First-slice <see cref="IPetService"/>. Wires the summon /
/// recall lifecycle and runs hunger decay on a coarse cadence.
///
/// Intimacy adjustments (heal / damage / interaction), per-pet skill
/// (<c>pet_attackskill</c>), runaway-on-hunger-zero, and the bonus
/// script chain land as their owning subsystems port.
/// </summary>
public sealed class PetService : IPetService
{
    /// <summary>rAthena <c>battle_config.pet_hungry_delay_rate</c> default = 60 s per −1 hunger.</summary>
    private const int HungerDecayIntervalMs = 60_000;

    private readonly IEntityRegistry _entities;
    private readonly IMobDb _mobDb;
    private readonly IStatusCalcService _statusCalc;
    private readonly IVisibilityService _visibility;
    private readonly EntityIdAllocator _ids;
    private readonly ILogger<PetService> _logger;
    private readonly Dictionary<int, EntityId> _ownerToPet = new();
    private long _nextHungerTick;

    public PetService(
        IEntityRegistry entities,
        IMobDb mobDb,
        IStatusCalcService statusCalc,
        IVisibilityService visibility,
        EntityIdAllocator ids,
        ILogger<PetService> logger)
    {
        _entities = entities;
        _mobDb = mobDb;
        _statusCalc = statusCalc;
        _visibility = visibility;
        _ids = ids;
        _logger = logger;
    }

    public PetEntity? Summon(PlayerEntity owner, int petClassId, string petName, int eggItemId = 0)
    {
        // Only one live pet at a time per owner (rAthena pc_setpet rule).
        if (_ownerToPet.ContainsKey(owner.CharacterId)) return null;
        var db = _mobDb.Get(petClassId);
        if (db == null) return null;

        var pet = new PetEntity(_ids.NextMob(), db, owner.MapId, owner.X, owner.Y)
        {
            PetName = string.IsNullOrEmpty(petName) ? db.Name : petName,
            EggId = eggItemId,
        };
        pet.MasterId = owner.Id;
        _statusCalc.CalcMob(pet);
        _entities.Add(pet);
        _ownerToPet[owner.CharacterId] = pet.Id;
        _visibility.NotifySpawnedToArea(pet);
        _logger.LogInformation(
            "Pet {Pet} (class {Class}) summoned for owner {Char}",
            pet.Id.Value, petClassId, owner.CharacterId);
        return pet;
    }

    public void Recall(PlayerEntity owner)
    {
        if (!_ownerToPet.Remove(owner.CharacterId, out var petId)) return;
        var pet = _entities.Get(petId);
        if (pet != null)
        {
            _visibility.NotifyVanishedToArea(pet, Core.Server.Packets.Out.ZC.VanishReason.Outsight);
            _entities.Remove(petId);
        }
    }

    public void Tick(long nowTick)
    {
        if (_ownerToPet.Count == 0) return;
        if (nowTick < _nextHungerTick) return;
        _nextHungerTick = nowTick + HungerDecayIntervalMs;

        foreach (var (charId, petId) in _ownerToPet.ToArray())
        {
            if (_entities.Get(petId) is not PetEntity pet) continue;
            if (pet.Hunger == 0)
            {
                // Runaway — rAthena pet_data_init(2): if hungry, pet flees.
                _logger.LogDebug("Pet {Pet} ran away (hunger zero, owner {Char})",
                    pet.Id.Value, charId);
                Recall(GetOwnerStub(charId));
                continue;
            }
            pet.Hunger--;
            // Intimacy decay on starving (rAthena PET_HUNGRY): −5 per tick at 0% hunger
            if (pet.Hunger < 10 && pet.Intimacy > 0)
            {
                pet.Intimacy = (ushort)Math.Max(0, pet.Intimacy - 5);
            }
        }
    }

    /// <summary>
    /// Sentinel for Recall when we only have the char id — Recall reads
    /// the dictionary by charId so a PlayerEntity shell suffices.
    /// </summary>
    private static PlayerEntity GetOwnerStub(int charId)
        => new(charId, 0, "", Guid.Empty, 0, 0, 0);

    /// <inheritdoc />
    public Core.Server.IPC.PetData? SerializeSnapshot(int petId)
    {
        // Walk live pets by persistent pet_id. rAthena's intif_save_petdata
        // reads off `sd->pd->pet` directly; the C# side maps char_id →
        // EntityId, so we scan owners. Cheap — at most one live pet per
        // PC, and the map process owns << 1k PCs typically.
        foreach (var (charId, entityId) in _ownerToPet)
        {
            if (_entities.Get(entityId) is not PetEntity pet) continue;
            if (pet.PetId != petId) continue;
            return ToPetData(pet, charId);
        }
        return null;
    }

    /// <inheritdoc />
    public bool TryGetLivePetId(PlayerEntity owner, out int petId)
    {
        petId = 0;
        if (!_ownerToPet.TryGetValue(owner.CharacterId, out var entityId)) return false;
        if (_entities.Get(entityId) is not PetEntity pet) return false;
        petId = (int)pet.PetId;
        return true;
    }

    /// <summary>
    /// Maps a live <see cref="PetEntity"/> to the
    /// <see cref="Core.Server.IPC.PetData"/> shape consumed by
    /// <c>PetSaveAsync</c>. Mirrors rAthena <c>pet_data_init</c> field
    /// projection.
    /// </summary>
    private static Core.Server.IPC.PetData ToPetData(PetEntity pet, int ownerCharId)
        => new()
        {
            PetId = (int)pet.PetId,
            AccountId = 0, // owner's account id resolved char-side from char_id
            CharacterId = ownerCharId,
            ClassId = pet.ClassId,
            Level = pet.Level,
            EggItemId = pet.EggId,
            EquipItemId = (int)pet.EquipItemId,
            Intimacy = pet.Intimacy,
            Hungry = pet.Hunger,
            RenameFlag = 0,
            Incubate = false,
            Name = pet.PetName ?? string.Empty,
        };
}
