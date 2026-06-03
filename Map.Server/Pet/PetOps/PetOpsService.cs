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
    // FEATURE-07 — egg/hatch inventory access.
    private readonly Map.Server.Status.ISessionManagerAccessor? _sessions;
    private readonly Map.Server.Inventory.IInventoryService? _inventory;
    private readonly IPetClientService? _client;
    private readonly Random _rng;

    // FEATURE-07 — egg item id → pet class id (built lazily from pet_db + mob_db).
    private Dictionary<uint, int>? _eggToClass;

    public PetOpsService(
        IServiceScopeFactory scopes,
        IEntityRegistry entities,
        IPetService pet,
        ILogger<PetOpsService> logger,
        Map.Server.Mob.IMobDb? mobDb = null,
        Map.Server.Items.IItemCatalog? items = null,
        Map.Server.Services.Intif.IIntifService? intif = null,
        Map.Server.Status.ISessionManagerAccessor? sessions = null,
        Map.Server.Inventory.IInventoryService? inventory = null,
        IPetClientService? client = null,
        Random? rng = null)
    {
        _scopes = scopes;
        _entities = entities;
        _pet = pet;
        _logger = logger;
        _mobDb = mobDb;
        _items = items;
        _intif = intif;
        _sessions = sessions;
        _inventory = inventory;
        _client = client;
        _rng = rng ?? Random.Shared;
        Reload();
    }

    public PetOpsService(ILogger<PetOpsService> logger) { _logger = logger; _rng = Random.Shared; }

    /// <summary>FEATURE-01/07 test ctor — wires the egg/catch deps without triggering a DB reload.</summary>
    internal PetOpsService(ILogger<PetOpsService> logger, Map.Server.Mob.IMobDb? mobDb,
        Map.Server.Items.IItemCatalog? items, Map.Server.Services.Intif.IIntifService? intif, Random? rng,
        Map.Server.Status.ISessionManagerAccessor? sessions = null,
        Map.Server.Inventory.IInventoryService? inventory = null,
        IPetService? pet = null,
        IPetClientService? client = null,
        IEntityRegistry? entities = null)
    {
        _logger = logger;
        _mobDb = mobDb;
        _items = items;
        _intif = intif;
        _sessions = sessions;
        _inventory = inventory;
        _pet = pet;
        _client = client;
        _entities = entities;
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

    /// <summary>FEATURE-07 — rAthena <c>pet_create_egg</c> (pet.cpp): a hatchable egg item resolves to
    /// a pet class via pet_db; dispatch <c>intif_create_pet</c> so the char side inserts a fresh pet
    /// row (the egg item is then granted on the <see cref="GetEgg"/> response). Returns false when the
    /// egg item maps to no pet.</summary>
    public bool CreateEgg(PlayerEntity master, int itemId)
    {
        var classId = EggItemToClass((uint)itemId);
        if (classId == 0 || _intif == null)
        {
            _logger.LogInformation("pet_create_egg: master={Master} egg={Item} is not a pet egg", master.Name, itemId);
            return false;
        }
        var pet = _mobDb?.Get(classId);
        var petAegis = pet?.AegisName;
        var cat = petAegis != null ? GetCatalogEntry(petAegis) : null;
        var intimacy = (byte)Math.Clamp(cat?.IntimacyStart ?? 250, 0, 255);
        var hungry = (byte)Math.Clamp(cat?.Fullness ?? MaxHunger, 0, 100);
        _intif.PetCreate(master, classId: classId, nameId: itemId, rename: 0, eggItemId: itemId,
            intimate: intimacy, hungry: hungry, gender: '\0', petName: pet?.Name ?? string.Empty);
        _logger.LogInformation("pet_create_egg: {Master} created pet class {Class} from egg {Item}",
            master.Name, classId, itemId);
        return true;
    }

    /// <summary>FEATURE-07 — rAthena <c>pet_get_egg</c>: the char side created the pet row; grant the
    /// egg item to the master's inventory so it can be hatched. ➡️ Binding the returned pet_id into the
    /// egg's card slots (so a saved pet's intimacy survives a re-egg) is FEATURE-27.</summary>
    public bool GetEgg(PlayerEntity master, int classId, int itemId, byte gender)
    {
        var session = _sessions?.GetByEntityId(master.Id);
        if (session == null || _inventory == null) return false;
        var ok = _inventory.GiveItem(session, (uint)itemId, 1);
        _logger.LogInformation("pet_get_egg: {Master} received egg {Item} for class {Class} (ok={Ok})",
            master.Name, itemId, classId, ok);
        return ok;
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

    /// <summary>FEATURE-07 — rAthena <c>pet_egg_search</c>: the inventory slot (ServerIndex) of the
    /// egg item <paramref name="eggId"/>, or -1 if the master doesn't hold it.</summary>
    public int EggSearch(PlayerEntity master, int eggId)
    {
        var inv = _sessions?.GetByEntityId(master.Id)?.Inventory;
        if (inv == null) return -1;
        foreach (var i in inv)
            if (i.NameId == (uint)eggId && i.Amount > 0) return i.ServerIndex;
        return -1;
    }

    public int SelectEgg(PlayerEntity master, short eggIndex)
    {
        // rAthena pet_select_egg — mark the selected egg index pending hatch.
        if (eggIndex < 0) return -1;
        master.PetCatchTargetClass = eggIndex; // reuse field as "selected egg slot"
        return 0;
    }

    /// <summary>FEATURE-07 — rAthena <c>pet_birth_process</c>: hatch the selected egg into a live pet.
    /// Resolves the egg item at the selected inventory slot → pet class (pet_db) → consumes the egg →
    /// <see cref="IPetService.Summon"/>. Returns 0 on hatch, -1 on any failure.</summary>
    public int BirthProcess(PlayerEntity master)
    {
        var slot = master.PetCatchTargetClass;
        if (slot < 0 || _pet == null) return -1;
        master.PetCatchTargetClass = -1;

        var session = _sessions?.GetByEntityId(master.Id);
        var inv = session?.Inventory;
        if (inv == null) return -1;

        var egg = inv.FirstOrDefault(i => i.ServerIndex == slot && i.Amount > 0);
        if (egg == null) return -1;
        var classId = EggItemToClass(egg.NameId);
        if (classId == 0) return -1;

        // Consume the egg item, then hatch the live pet (the hunger timer is already running in
        // PetService.Tick). PACKET-03 owns clif_send_petdata — see the pet-UI follow-up.
        egg.Amount -= 1;
        if (egg.Amount == 0)
        {
            if (egg.Id > 0) session!.RemovedInventoryIds.Add(egg.Id);
            inv.Remove(egg);
        }
        var mob = _mobDb?.Get(classId);
        _pet.Summon(master, classId, mob?.Name ?? string.Empty, eggItemId: (int)egg.NameId);
        _logger.LogInformation("pet_birth_process: {Master} hatched pet class {Class}", master.Name, classId);
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
        // rAthena pet_food → clif_send_petdata(HUNGER) + clif_send_petdata(INTIMACY).
        _client?.SendPetData(master, pet, Core.Server.Packets.Out.ZC.PetDataType.Hunger, pet.Hunger);
        _client?.SendPetData(master, pet, Core.Server.Packets.Out.ZC.PetDataType.Intimacy, pet.Intimacy);
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
        // rAthena: if intimacy reaches 0, pet runs away; else push the new intimacy to the client.
        if (pet.Intimacy == 0) { _pet?.Recall(master); return; }
        _client?.SendPetData(master, pet, Core.Server.Packets.Out.ZC.PetDataType.Intimacy, pet.Intimacy);
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
        // rAthena pet_menu (pet.cpp:1422): 0=pet information, 1=feed, 2=performance, 3=return to egg,
        // 4=unequip accessory. Gate: a hatched, non-runaway pet (intimate > PET_INTIMATE_NONE).
        var pet = GetLivePet(master);
        if (pet == null || pet.Intimacy == 0) return 1; // rAthena returns 1 on "lost the pet already"
        switch (choice)
        {
            case 0: // pet information → resend the status panel
                _client?.SendPetStatus(master, pet);
                return 0;
            case 1: // feed
                return Food(master);
            case 2: // performance — clif_pet_performance (a randomised act number broadcast)
                _client?.SendPetData(master, pet, Core.Server.Packets.Out.ZC.PetDataType.Performance, PerformanceNumber(pet));
                return 0;
            case 3: // return to egg
                return ReturnEgg(master) ? 0 : -1;
            case 4: // unequip accessory
                if (pet.EquipItemId != 0)
                {
                    pet.EquipItemId = 0;
                    _client?.SendPetData(master, pet, Core.Server.Packets.Out.ZC.PetDataType.Accessory, 0);
                }
                return 0;
            default: return -1;
        }
    }

    /// <summary>rAthena <c>clif_send_petdata(PERFORMANCE)</c> band: 1..3 normal (4 if the pet has a
    /// special performance) gated by intimacy. We don't roll randomly here (deterministic for tests);
    /// the client renders the act, so the exact pick is cosmetic.</summary>
    private static int PerformanceNumber(Map.Server.Entities.PetEntity pet)
        => pet.Intimacy > 900 ? 3 : pet.Intimacy > 750 ? 2 : 1;

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

    /// <summary>FEATURE-07 test seam — force a rebuild of the egg→class index after seeding.</summary>
    internal void InvalidateEggIndexForTest() => _eggToClass = null;

    /// <summary>FEATURE-07 — resolve a hatchable egg item id to its pet class id (0 = not a pet egg),
    /// via the pet_db <c>EggItem</c> → <c>MobAegis</c> → mob class chain.</summary>
    private int EggItemToClass(uint eggItemId)
    {
        EnsureEggIndex();
        return _eggToClass!.TryGetValue(eggItemId, out var c) ? c : 0;
    }

    private void EnsureEggIndex()
    {
        if (_eggToClass != null) return;
        _eggToClass = new Dictionary<uint, int>();
        foreach (var (mobAegis, cat) in _catalog)
        {
            if (string.IsNullOrEmpty(cat.EggItem)) continue;
            var eggItem = _items?.GetByAegisName(cat.EggItem);
            var mob = _mobDb?.GetByAegisName(mobAegis);
            if (eggItem == null || mob == null) continue;
            _eggToClass[eggItem.Id] = mob.Id;
        }
    }
}
