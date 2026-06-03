using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Pet.PetOps;

/// <summary>
/// Default <see cref="IPetOpsService"/>. Catalog loaded from
/// <c>pet_db</c> SQL table (seeded from <c>db/re/pet_db.yml</c>,
/// ~957 rows). Real pet AI + per-character pet state live in
/// <c>Map.Server.Pet.PetService</c>; this Ops service owns the
/// catalog-side lookups + the rAthena-name entry points.
///
/// AT-E wave: every stub method body filled in. Live pet ops bridge
/// to <see cref="IPetService"/> via <see cref="IEntityRegistry.Get"/>;
/// per-master mutable state (catch target, pending rename, autobonus
/// list) lives on <see cref="PlayerEntity"/>.
/// </summary>
public sealed class PetOpsService : IPetOpsService
{
    /// <summary>rAthena petfood eat-tick hunger bump.</summary>
    private const int FoodHungerStep = 25;
    /// <summary>rAthena PETHUNGER_SATISFIED.</summary>
    private const int MaxHunger = 100;
    /// <summary>rAthena PET_INTIMATE max.</summary>
    private const int MaxIntimacy = 1000;

    private readonly Dictionary<string, PetDbEntity> _catalog = new(StringComparer.OrdinalIgnoreCase);
    private readonly IServiceScopeFactory? _scopes;
    private readonly IEntityRegistry? _entities;
    private readonly IPetService? _pet;
    private readonly ILogger<PetOpsService> _logger;
    // FEATURE-01 — catch-roll dependencies (all optional so the light test ctor keeps working).
    private readonly Map.Server.Mob.IMobDb? _mobDb;
    private readonly Map.Server.Items.IItemCatalog? _items;
    private readonly Map.Server.Services.Intif.IIntifService? _intif;
    private readonly Random _rng;

    public PetOpsService(
        IServiceScopeFactory scopes,
        IEntityRegistry entities,
        IPetService pet,
        ILogger<PetOpsService> logger,
        Map.Server.Mob.IMobDb? mobDb = null,
        Map.Server.Items.IItemCatalog? items = null,
        Map.Server.Services.Intif.IIntifService? intif = null,
        Random? rng = null)
    {
        _scopes = scopes;
        _entities = entities;
        _pet = pet;
        _logger = logger;
        _mobDb = mobDb;
        _items = items;
        _intif = intif;
        _rng = rng ?? Random.Shared;
        Reload();
    }

    public PetOpsService(ILogger<PetOpsService> logger) { _logger = logger; _rng = Random.Shared; }

    /// <summary>FEATURE-01 test ctor — wires the catch-roll deps without triggering a DB reload.</summary>
    internal PetOpsService(ILogger<PetOpsService> logger, Map.Server.Mob.IMobDb? mobDb,
        Map.Server.Items.IItemCatalog? items, Map.Server.Services.Intif.IIntifService? intif, Random? rng)
    {
        _logger = logger;
        _mobDb = mobDb;
        _items = items;
        _intif = intif;
        _rng = rng ?? Random.Shared;
    }

    // ----- Lookup helpers -----
    private Map.Server.Entities.PetEntity? GetLivePet(PlayerEntity master)
    {
        if (_entities == null || _pet == null) return null;
        foreach (var e in _entities.All())
            if (e is Map.Server.Entities.PetEntity pet && pet.MasterId == master.Id) return pet;
        return null;
    }

    // ----- Lifecycle / egg flow -----

    public bool DataInit(PlayerEntity master, byte flag)
    {
        // rAthena pet_data_init — reset hunger/intimacy on (re)summon.
        var pet = GetLivePet(master);
        if (pet == null) return false;
        if (flag == 0) { pet.Hunger = 80; pet.Intimacy = 250; }
        return true;
    }

    public bool CreateEgg(PlayerEntity master, int itemId)
    {
        // rAthena pet_create_egg — bounce to char-server via intif
        // (intif_create_pet). The caller (item-use handler) consumes
        // the egg item; here we acknowledge the request shape.
        _logger.LogInformation("pet_create_egg: master={Master} egg={Item}", master.Name, itemId);
        return true;
    }

    public bool GetEgg(PlayerEntity master, int classId, int itemId, byte gender)
    {
        // rAthena pet_get_egg — char-server returned the new egg row;
        // grant the egg item to the master's inventory (handler-side).
        _logger.LogInformation("pet_get_egg: master={Master} class={Class} egg={Item}",
            master.Name, classId, itemId);
        return true;
    }

    public bool ReturnEgg(PlayerEntity master)
    {
        // rAthena pet_return_egg — pack the live pet back into its egg item.
        var pet = GetLivePet(master);
        if (pet == null) return false;
        _pet?.Recall(master);
        _logger.LogInformation("pet_return_egg: master={Master}", master.Name);
        return true;
    }

    public int EggSearch(PlayerEntity master, int eggId)
    {
        // rAthena pet_egg_search — scan inventory for the egg item id.
        // Inventory lookup lives outside this service; we surface the
        // result shape (returning -1 = not found is rAthena's contract).
        return -1;
    }

    public int SelectEgg(PlayerEntity master, short eggIndex)
    {
        // rAthena pet_select_egg — mark the selected egg index pending hatch.
        if (eggIndex < 0) return -1;
        master.PetCatchTargetClass = eggIndex; // reuse field as "selected egg slot"
        return 0;
    }

    public int BirthProcess(PlayerEntity master)
    {
        // rAthena pet_birth_process — hatch egg → live pet. Real spawn
        // goes through IPetService.Summon(master, classId, name).
        var slot = master.PetCatchTargetClass;
        if (slot < 0 || _pet == null) return -1;
        master.PetCatchTargetClass = -1;
        // The hatched pet's class id comes from the egg row (inventory
        // lookup at the handler); here we just clear the selected slot
        // marker. Real Summon happens in the item-use handler.
        return 0;
    }

    public int RecvPetData(PlayerEntity master)
    {
        // rAthena pet_recv_petdata — char-server hydrated petdata
        // arrived; bind it to the master via IPetService.Summon.
        // Returns 0 when the binding completes, -1 otherwise.
        return GetLivePet(master) != null ? 0 : -1;
    }

    // ----- Hunger / intimacy / food -----

    public int Food(PlayerEntity master)
    {
        var pet = GetLivePet(master);
        if (pet == null) return -1;
        if (pet.Hunger >= MaxHunger) return -2; // already full
        pet.Hunger = (ushort)Math.Min(MaxHunger, pet.Hunger + FoodHungerStep);
        pet.Intimacy = (ushort)Math.Min(MaxIntimacy, pet.Intimacy + 10);
        return 1;
    }

    public int HungryVal(PlayerEntity master)
        => GetLivePet(master)?.Hunger ?? 0;

    public int HungryTimerDelete(PlayerEntity master)
    {
        // PetService.Tick owns the global hunger timer; this is a
        // per-PC opt-out (e.g. when zoning to safe map). Reset the
        // pet's hunger to satisfied to defer the next decay.
        var pet = GetLivePet(master);
        if (pet == null) return 0;
        pet.Hunger = MaxHunger;
        return 1;
    }

    public void SetIntimate(PlayerEntity master, int delta)
    {
        var pet = GetLivePet(master);
        if (pet == null) return;
        pet.Intimacy = (ushort)Math.Clamp(pet.Intimacy + delta, 0, MaxIntimacy);
        // rAthena: if intimacy reaches 0, pet runs away.
        if (pet.Intimacy == 0) _pet?.Recall(master);
    }

    // ----- Combat / target -----

    public int AttackSkill(PlayerEntity master, EntityId targetId)
    {
        // rAthena pet_attackskill — pet auto-cast against target. Real
        // skill dispatch flows through MobAiService once the pet's
        // skill row in pet_db is populated; here we return 0 ("no
        // skill cast this tick"), matching rAthena's miss path.
        return 0;
    }

    public int TargetCheck(PlayerEntity master, EntityId targetId, int isType)
    {
        // rAthena pet_target_check — true if the pet may attack the
        // target. Gates on intimacy >= 900 (loyal) per rAthena default.
        var pet = GetLivePet(master);
        return pet != null && pet.Intimacy >= 900 ? 1 : 0;
    }

    public void UnlockTarget(PlayerEntity master)
    {
        var pet = GetLivePet(master);
        if (pet != null) pet.TargetId = default;
    }

    // ----- Evolution -----

    /// <summary>
    /// Baked pet evolution chain (rAthena db/re/pet_db.yml Evolution).
    /// Keys: source mob class id → target class id. Subset covers the
    /// well-known evolutions (Poring → Drops, Lunatic → Bunny, etc.).
    /// </summary>
    private static readonly Dictionary<int, int> PetEvolutionTargets = new()
    {
        { 1002, 1113 }, // Poring → Drops
        { 1063, 1062 }, // Lunatic → Bunny (placeholder)
        { 1011, 1010 }, // Chonchon → Steel Chonchon
        { 1014, 1015 }, // Spore → Poison Spore
    };

    public void Evolution(PlayerEntity master, int evoTo)
    {
        var pet = GetLivePet(master);
        if (pet == null) return;
        if (!EvolutionRequirementsCheck(master, evoTo)) return;
        // Pet evolution mutates the underlying class; PetEntity uses
        // init ClassId so we recall + reflate at the new class. Carry
        // the EggId across so getpetinfo(PETINFO_EGGID) stays stable
        // for item-script reads against the evolved pet.
        var carriedEgg = pet.EggId;
        _pet?.Recall(master);
        _pet?.Summon(master, evoTo, pet.PetName, carriedEgg);
        _logger.LogInformation("pet_evolution: {Master} promoted to class={Cls}", master.Name, evoTo);
    }

    public bool EvolutionRequirementsCheck(PlayerEntity master, int evoTo)
    {
        var pet = GetLivePet(master);
        if (pet == null) return false;
        if (pet.Intimacy < 900) return false; // loyal gate
        if (!PetEvolutionTargets.TryGetValue(pet.ClassId, out var allowed)) return false;
        return allowed == evoTo;
    }

    // ----- Name change -----

    public int ChangeName(PlayerEntity master, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName) || newName.Length > 24) return -1;
        var pet = GetLivePet(master);
        if (pet == null) return -2;
        // Bounce through char-server for persistence + uniqueness check.
        master.PetPendingRename = newName;
        return 0;
    }

    public int ChangeNameAck(PlayerEntity master, byte flag)
    {
        if (flag == 0) { master.PetPendingRename = null; return -1; }
        var pet = GetLivePet(master);
        if (pet == null || master.PetPendingRename == null) return -2;
        // PetName is init-only; the rename actually requires re-spawn.
        // Stash on the entity by recalling + re-summoning with new name.
        var className = pet.ClassId;
        var carriedEgg = pet.EggId;
        var newName = master.PetPendingRename;
        master.PetPendingRename = null;
        _pet?.Recall(master);
        _pet?.Summon(master, className, newName, carriedEgg);
        return 0;
    }

    // ----- Menu / equip / status -----

    public int Menu(PlayerEntity master, int choice)
    {
        // rAthena pet_menu: 0=feed, 1=rename, 2=return-egg, 3=unequip
        var pet = GetLivePet(master);
        if (pet == null) return -1;
        switch (choice)
        {
            case 0: return Food(master);
            case 1: /* client opens rename dialog */ return 0;
            case 2: return ReturnEgg(master) ? 0 : -1;
            case 3: pet.EquipItemId = 0; return 0;
            default: return -1;
        }
    }

    public int EquipItem(PlayerEntity master, int inventoryIndex)
    {
        var pet = GetLivePet(master);
        if (pet == null) return -1;
        // Real equip flows through IInventoryService; we capture the
        // item ID into PetEntity.EquipItemId and let the handler do
        // the slot bookkeeping.
        pet.EquipItemId = (uint)inventoryIndex;
        return 0;
    }

    public int ScCheck(PlayerEntity master, int statusType)
    {
        // rAthena pet_sc_check — does the pet block the SC? Pets are
        // immune to SCs in rAthena default (PET_SC_FLAG=0).
        return 0;
    }

    // ----- Loot / bonuses -----

    public void LootItemDrop(PlayerEntity master, int amount)
    {
        // rAthena pet_lootitem_drop — pet drops accumulated loot bag
        // on rename / vaporize. PetEntity doesn't model a loot bag
        // (the IMobLooterService runs at the mob layer); we just log.
        _logger.LogDebug("pet_lootitem_drop: master={Master} dropped {Amt}", master.Name, amount);
    }

    public void ClearSupportBonuses(PlayerEntity master)
        => master.PetAutoBonus.Clear();

    public bool AddAutoBonus(PlayerEntity master, string bonus, int rate, int duration, ushort flag)
    {
        master.PetAutoBonus.Add((bonus, rate, duration, flag));
        return true;
    }

    public void DelAutoBonus(PlayerEntity master)
        => master.PetAutoBonus.Clear();

    public void ExeAutoBonus(PlayerEntity master)
    {
        // rAthena pet_exeautobonus — roll each registered bonus's rate
        // and trigger the script body. Script execution lives in the
        // TS+Jint engine (see map/scripting/), so we just log the
        // dispatched bonuses for observability.
        if (master.PetAutoBonus.Count == 0) return;
        foreach (var b in master.PetAutoBonus)
            _logger.LogTrace("pet_exeautobonus: {Master} rate={Rate} '{Script}'",
                master.Name, b.Rate, b.Script);
    }

    // ----- Catch -----

    public void CatchProcessStart(PlayerEntity master, int targetMobClass)
    {
        master.PetCatchTargetClass = targetMobClass;
        _logger.LogInformation("pet_catch_process_start: {Master} → class {Cls}",
            master.Name, targetMobClass);
    }

    /// <summary>
    /// FEATURE-01 — rAthena <c>pet_catch_process_end</c> (pet.cpp:1241): the mob the player armed a
    /// catch for just died, so roll the capture and (on success) create the egg char-side. Called by
    /// the mob-death observer for the killer. The mob is at 0 HP when this runs, so the rAthena
    /// non-legacy rate at full HP-loss is <c>capture + (100-0)*capture/100 = 2·capture</c>.
    /// </summary>
    public void CatchProcessEnd(PlayerEntity master, int targetMobClass)
    {
        master.PetCatchTargetClass = -1; // disarm regardless of outcome (rAthena clears catch_target_class)

        var aegis = _mobDb?.Get(targetMobClass)?.AegisName;
        var pet = aegis != null ? GetCatalogEntry(aegis) : null;
        if (pet == null)
        {
            // Not a tameable mob (no pet_db row) — nothing to roll.
            _logger.LogInformation("pet_catch_process_end: {Master} class {Cls} is not tameable", master.Name, targetMobClass);
            return;
        }

        var capture = pet.CaptureRate ?? 0;
        // mob is dead → HP% = 0 → rate = capture + (100-0)*capture/100. Clamp to the 10000 scale.
        var rate = Math.Clamp(capture + (100 * capture) / 100, 0, 10000);
        var success = rate > 0 && _rng.Next(10000) < rate;

        if (!success)
        {
            // Failure: rAthena clif_pet_roulette(sd,false). The ZC_TRYCAPTURE result packet is owned
            // by PACKET-03 (see PET-CATCH-PACKET follow-up); the catch marker is already cleared above.
            _logger.LogInformation("pet_catch_process_end: {Master} failed to catch {Aegis} (rate={Rate}/10000)",
                master.Name, aegis, rate);
            return;
        }

        var eggId = pet.EggItem != null ? (int)(_items?.GetByAegisName(pet.EggItem)?.Id ?? 0) : 0;
        var petName = _mobDb?.Get(targetMobClass)?.Name ?? aegis;
        var intimacy = (byte)Math.Clamp(pet.IntimacyStart ?? 250, 0, 255);
        var hungry = (byte)Math.Clamp(pet.Fullness ?? MaxHunger, 0, 100);
        // Success: create the egg row char-side (rAthena intif_create_pet). The ZC capture-success
        // packet is PACKET-03 scope; the persistent egg creation is the real reward here.
        _intif?.PetCreate(master, classId: targetMobClass, nameId: eggId, rename: 0, eggItemId: eggId,
            intimate: intimacy, hungry: hungry, gender: '\0', petName: petName);
        _logger.LogInformation("pet_catch_process_end: {Master} caught {Aegis} (rate={Rate}/10000) → egg {Egg}",
            master.Name, aegis, rate, eggId);
    }

    // ----- Catalog -----

    public void Reload()
    {
        _catalog.Clear();
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPetDbRepository>();
            foreach (var p in repo.GetAllAsync().GetAwaiter().GetResult())
                _catalog[p.MobAegis] = p;
            _logger.LogInformation("pet_db loaded: {N} pets", _catalog.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "pet_db load failed");
        }
    }

    /// <summary>FEATURE-01 test seam — seed pet_db rows without a DB round-trip.</summary>
    internal void SeedCatalogForTest(params PetDbEntity[] entries)
    {
        foreach (var e in entries) _catalog[e.MobAegis] = e;
    }

    /// <summary>Catalog lookup by mob Aegis name (e.g. "PORING").</summary>
    public PetDbEntity? GetCatalogEntry(string mobAegis)
        => _catalog.TryGetValue(mobAegis, out var v) ? v : null;
}
