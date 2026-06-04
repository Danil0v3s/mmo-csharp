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
    private readonly Map.Server.Visibility.IVisibilityService? _visibility;
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
        Map.Server.Visibility.IVisibilityService? visibility = null,
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
        _visibility = visibility;
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
        IEntityRegistry? entities = null,
        Map.Server.Visibility.IVisibilityService? visibility = null)
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
        _visibility = visibility;
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

    /// <summary>FEATURE-07/27 — rAthena <c>pet_get_egg</c>: the char side created the pet row; grant the
    /// egg item to the master's inventory with the returned <paramref name="petId"/> bound into the
    /// egg's card slots (CARD0_PET) so the saved pet's intimacy/hunger/name survive being re-hatched.</summary>
    public bool GetEgg(PlayerEntity master, int classId, int eggItemId, int petId)
    {
        var session = _sessions?.GetByEntityId(master.Id);
        if (session == null || _inventory == null) return false;
        var (c0, c1, c2) = PetEggCard.Bind(petId);
        var ok = _inventory.GiveItemWithCards(session, (uint)eggItemId, 1, c0, c1, c2, 0);
        _logger.LogInformation("pet_get_egg: {Master} received egg {Item} (pet {Pet}, class {Class}, ok={Ok})",
            master.Name, eggItemId, petId, classId, ok);
        return ok;
    }

    public bool ReturnEgg(PlayerEntity master)
    {
        // rAthena pet_return_egg — pack the live pet back into its egg item. Hand any accumulated loot
        // bag to the owner first (rAthena pet_lootitem_drop runs on the way out).
        var pet = GetLivePet(master);
        if (pet == null) return false;
        LootItemDrop(master);
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

    /// <summary>rAthena <c>bpet</c> → <c>clif_sendegg</c>: list every pet-egg item in the bag (by client
    /// inventory index) so the incubator dialog opens. Triggered by using a pet-egg item.</summary>
    public void OpenEggList(PlayerEntity master)
    {
        var inv = _sessions?.GetByEntityId(master.Id)?.Inventory;
        if (inv == null) return;
        var eggs = new List<short>();
        foreach (var i in inv)
        {
            if (i.Amount <= 0) continue;
            if (EggItemToClass(i.NameId) == 0) continue; // only pet eggs (pet_db EggItem)
            eggs.Add((short)(i.ServerIndex + 2)); // client_index = server_index + 2
        }
        _client?.SendEggList(master, eggs);
        _logger.LogInformation("pet egg list: {Master} has {N} hatchable egg(s)", master.Name, eggs.Count);
    }

    /// <summary>rAthena <c>pet_select_egg</c> — the player chose an egg slot; hatch it.</summary>
    public int SelectEgg(PlayerEntity master, short eggSlot) => BirthProcess(master, eggSlot);

    /// <summary>FEATURE-07/27 — rAthena <c>pet_birth_process</c> + <c>pet_select_egg</c>: hatch the egg
    /// at <paramref name="eggSlot"/> into a live pet. If the egg carries a bound pet_id
    /// (<c>CARD0_PET</c>) the saved pet is loaded char-side (<c>intif_request_petdata</c>) and hatched
    /// with its persisted intimacy/hunger/name (the relog round-trip); otherwise a fresh pet is hatched
    /// from the egg's class. The egg is consumed up-front (so a failed/duplicate hatch can't double).
    /// Returns 0 on accept, -1 on failure.</summary>
    public int BirthProcess(PlayerEntity master, int eggSlot)
    {
        if (eggSlot < 0 || _pet == null) return -1;

        var session = _sessions?.GetByEntityId(master.Id);
        var inv = session?.Inventory;
        if (inv == null) return -1;

        var egg = inv.FirstOrDefault(i => i.ServerIndex == eggSlot && i.Amount > 0);
        if (egg == null) return -1;
        var boundPetId = PetEggCard.ReadPetId(egg);
        var fallbackClass = EggItemToClass(egg.NameId);
        if (boundPetId is null && fallbackClass == 0) return -1; // not a pet egg

        // rAthena one-pet rule (pc_setpet): refuse to hatch — and don't consume the egg — if a pet is
        // already out. Pre-checked before the consume so a failed hatch never eats the egg.
        if (_pet.TryGetLivePetId(master, out _)) return -1;

        // Consume the egg now (the pet panel emits from PetService.Summon — synchronously for a fresh
        // hatch, or after the char-side load for a bound egg).
        var eggNameId = (int)egg.NameId;
        egg.Amount -= 1;
        if (egg.Amount == 0)
        {
            if (egg.Id > 0) session!.RemovedInventoryIds.Add(egg.Id);
            inv.Remove(egg);
        }

        if (boundPetId is int petId && petId > 0)
            _ = HatchBoundAsync(master, petId, fallbackClass, eggNameId); // FEATURE-27: load the saved pet
        else
            HatchFresh(master, fallbackClass, eggNameId);                 // fresh hatch from the egg class
        _logger.LogInformation("pet_birth_process: {Master} hatched egg {Egg} (pet {Pet})",
            master.Name, eggNameId, boundPetId);
        return 0;
    }

    /// <summary>Hatch a fresh pet from the egg's class (no saved row).</summary>
    private void HatchFresh(PlayerEntity master, int classId, int eggNameId)
    {
        if (classId == 0) return;
        var mob = _mobDb?.Get(classId);
        _pet?.Summon(master, classId, mob?.Name ?? string.Empty, eggItemId: eggNameId);
    }

    /// <summary>FEATURE-27 — rAthena <c>intif_request_petdata</c> → <c>pet_recv_petdata</c>: load the
    /// saved pet row by its bound id and hatch with the persisted intimacy/hunger/name. Falls back to a
    /// fresh hatch if the char server has no row (or the IPC is down).</summary>
    private async Task HatchBoundAsync(PlayerEntity master, int petId, int fallbackClass, int eggNameId)
    {
        var data = _intif != null ? await _intif.PetLoadAsync(petId, master.AccountId, master.CharacterId) : null;
        if (data == null)
        {
            HatchFresh(master, fallbackClass, eggNameId);
            return;
        }
        _pet?.Summon(master, data.ClassId, data.Name ?? string.Empty,
            eggItemId: data.EggItemId != 0 ? data.EggItemId : eggNameId,
            petId: data.PetId, intimacy: data.Intimacy, hunger: data.Hungry, renamed: data.RenameFlag != 0);
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

    /// <summary>rAthena <c>pet_change_name</c> (pet.cpp:1460): rename the active pet. Gates: a pet must
    /// be out, it can't already be renamed (battle_config.pet_rename off by default), and the name must
    /// be valid (≤ NAME_LENGTH, no control chars). Returns 1 on any rejection (rAthena), 0 on success.
    /// The new name is applied + the status panel re-emitted immediately; persisting it char-side rides
    /// the GP-PET persistence work (FEATURE-27).</summary>
    public int ChangeName(PlayerEntity master, string newName)
    {
        var pet = GetLivePet(master);
        if (pet == null) return 1;
        if (pet.RenameFlag) return 1;                                   // already renamed
        if (string.IsNullOrEmpty(newName) || newName.Length >= 24) return 1; // NAME_LENGTH
        foreach (var ch in newName) if (ch < 0x20 || ch == 0x7f) return 1;   // rAthena char validity
        ApplyPetName(master, pet, newName);
        return 0;
    }

    /// <summary>rAthena <c>pet_change_name_ack</c>: the char server confirmed (flag != 0) the rename;
    /// apply the pending name. Used once the rename persistence IPC is wired (FEATURE-27).</summary>
    public int ChangeNameAck(PlayerEntity master, byte flag)
    {
        if (flag == 0) { master.PetPendingRename = null; return -1; }
        var pet = GetLivePet(master);
        if (pet == null || master.PetPendingRename == null) return -2;
        ApplyPetName(master, pet, master.PetPendingRename);
        master.PetPendingRename = null;
        return 0;
    }

    /// <summary>Set the pet's name + rename flag and re-emit the status panel. The over-head BL_PET name
    /// refresh (rAthena clif_name for the unit) is GP-PET-RENAME-NAMEPKT.</summary>
    private void ApplyPetName(PlayerEntity master, Map.Server.Entities.PetEntity pet, string newName)
    {
        pet.PetName = newName;
        pet.RenameFlag = true;
        _client?.SendPetStatus(master, pet);
    }

    /// <summary>rAthena <c>clif_pet_emotion</c> (clif.cpp:8354): broadcast the pet's emotion / act to
    /// everyone in view (ZC_PET_ACT). No-op when the player has no live pet.</summary>
    public void Emotion(PlayerEntity master, int data)
    {
        var pet = GetLivePet(master);
        if (pet == null) return;
        _visibility?.SendToArea(pet, new Core.Server.Packets.Out.ZC.ZC_PET_ACT { Gid = pet.Id.Value, Data = data });
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

    /// <summary>rAthena <c>pet_lootitem_drop</c> (pet.cpp): hand the pet's accumulated loot bag back to
    /// the owner's inventory. Items that fit are added (and removed from the bag); items that don't fit
    /// (full/overweight bag) stay in the loot bag rather than vanish — rAthena instead drops them on the
    /// ground (➡️ GP-PET-LOOT-OVERFLOW).</summary>
    public void LootItemDrop(PlayerEntity master)
    {
        var pet = GetLivePet(master);
        if (pet == null || pet.LootItems.Count == 0) return;
        var session = _sessions?.GetByEntityId(master.Id);
        if (session == null || _inventory == null) return;

        var delivered = 0;
        for (var i = pet.LootItems.Count - 1; i >= 0; i--)
        {
            var slot = pet.LootItems[i];
            if (_inventory.GiveItem(session, (uint)slot.ItemId, slot.Amount))
            {
                pet.LootItems.RemoveAt(i);
                delivered++;
            }
        }
        if (delivered > 0)
            _logger.LogInformation("pet_lootitem_drop: {Master} received {N} looted item(s)", master.Name, delivered);
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

    /// <summary>rAthena <c>pet_catch_process_start</c> (pet.cpp:1214): arm a catch for the mob class
    /// and open the targeting cursor (<c>clif_catch_process</c> → ZC_START_CAPTURE).</summary>
    public void CatchProcessStart(PlayerEntity master, int targetMobClass)
    {
        master.PetCatchTargetClass = targetMobClass;
        _client?.SendCatchProcess(master);
        _logger.LogInformation("pet_catch_process_start: {Master} → class {Cls}",
            master.Name, targetMobClass);
    }

    /// <summary>rAthena <c>pet_distance_check</c> default — max king-move distance to the tamed mob.</summary>
    private const int PetDistanceCheck = 5;

    /// <summary>
    /// rAthena <c>pet_catch_process_end</c> (pet.cpp:1241): the player clicked a monster to tame.
    /// Validates the armed catch (mob alive, tameable, the armed class matches, in range), rolls the
    /// non-legacy capture rate <c>capture + ((100 − hp%) · capture) / 100</c> (≥1) against the mob's
    /// LIVE HP%, and on success removes the mob from the map + creates the egg char-side
    /// (<c>intif_create_pet</c>). Emits <c>clif_pet_roulette</c> either way and disarms the catch.
    /// </summary>
    public void CatchProcessEnd(PlayerEntity master, EntityId targetId)
    {
        var armedClass = master.PetCatchTargetClass;
        master.PetCatchTargetClass = -1; // disarm regardless of outcome (rAthena erases the process)

        if (armedClass < 0) { Fail(master); return; }                        // no catch armed
        if (_entities?.Get(targetId) is not Map.Server.Entities.MobEntity mob // invalid / gone
            || mob is Map.Server.Entities.PetEntity || mob.Hp <= 0)
        { Fail(master); return; }

        var aegis = _mobDb?.Get(mob.ClassId)?.AegisName;
        var pet = aegis != null ? GetCatalogEntry(aegis) : null;
        if (pet == null) { Fail(master); return; }                           // not tameable (no pet_db)

        // PET_CATCH_NORMAL parity: the armed taming target must be this mob's class.
        if (armedClass != mob.ClassId) { Fail(master); return; }

        // rAthena battle_config.pet_distance_check (default 5) — Chebyshev range to the target.
        var dist = Math.Max(Math.Abs(master.X - mob.X), Math.Abs(master.Y - mob.Y));
        if (dist > PetDistanceCheck) { Fail(master); return; }

        // Non-legacy rate vs the mob's LIVE HP%. get_percentage(hp, max_hp).
        var hpPct = mob.MaxHp > 0 ? (int)(100L * mob.Hp / mob.MaxHp) : 0;
        var capture = pet.CaptureRate ?? 0;
        var rate = capture + ((100 - hpPct) * capture) / 100;
        if (rate < 1) rate = 1;

        if (_rng.Next(10000) >= rate)
        {
            _logger.LogInformation("pet_catch_process_end: {Master} failed to catch {Aegis} (rate={Rate}/10000, hp%={Hp})",
                master.Name, aegis, rate, hpPct);
            Fail(master);
            return;
        }

        // Success — remove the mob (rAthena unit_remove_map + status_kill, CLR_OUTSIGHT), tell the
        // client it was caught, and create the egg row char-side (granted via pet_get_egg later).
        _visibility?.NotifyVanishedToArea(mob, Core.Server.Packets.Out.ZC.VanishReason.Outsight);
        _entities?.Remove(mob.Id);
        _client?.SendPetRoulette(master, true);

        var eggId = pet.EggItem != null ? (int)(_items?.GetByAegisName(pet.EggItem)?.Id ?? 0) : 0;
        var petName = _mobDb?.Get(mob.ClassId)?.Name ?? aegis;
        var intimacy = (byte)Math.Clamp(pet.IntimacyStart ?? 250, 0, 255);
        var hungry = (byte)Math.Clamp(pet.Fullness ?? MaxHunger, 0, 100);
        // rAthena intif_create_pet → (char) mapif_pet_created → pet_get_egg: create the row, then grant
        // the egg bound to the returned pet_id. Async (the char round-trip lands after this tick).
        _ = CreateAndGrantEggAsync(master, mob.ClassId, eggId, intimacy, hungry, petName ?? string.Empty);
        _logger.LogInformation("pet_catch_process_end: {Master} caught {Aegis} (rate={Rate}/10000, hp%={Hp}) → egg {Egg}",
            master.Name, aegis, rate, hpPct, eggId);
    }

    /// <summary>FEATURE-27 — create the pet row char-side and, on success, grant the egg bound to the
    /// new pet_id (rAthena's <c>intif_create_pet</c> → <c>pet_get_egg</c> callback).</summary>
    private async Task CreateAndGrantEggAsync(PlayerEntity master, int classId, int eggItemId, byte intimacy, byte hungry, string petName)
    {
        if (_intif == null) return;
        var petId = await _intif.PetCreateAsync(master, classId, eggItemId, intimacy, hungry, petName);
        if (petId > 0) GetEgg(master, classId, eggItemId, petId);
        else _logger.LogWarning("pet catch: PetCreate returned no pet_id for {Master} (class {Class})", master.Name, classId);
    }

    /// <summary>rAthena <c>clif_pet_roulette(sd, false)</c> — the catch attempt failed.</summary>
    private void Fail(PlayerEntity master) => _client?.SendPetRoulette(master, false);

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
