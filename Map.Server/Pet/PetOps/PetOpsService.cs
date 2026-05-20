using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Pet.PetOps;

/// <summary>Default <see cref="IPetOpsService"/>. Shells — real logic lives in PetService (Map.Server.Pet).</summary>
public sealed class PetOpsService : IPetOpsService
{
    private readonly ILogger<PetOpsService> _logger;
    public PetOpsService(ILogger<PetOpsService> logger) => _logger = logger;

    public bool DataInit(PlayerEntity master, byte flag) => false;
    public bool CreateEgg(PlayerEntity master, int itemId) => false;
    public bool GetEgg(PlayerEntity master, int classId, int itemId, byte gender) => false;
    public bool ReturnEgg(PlayerEntity master) => false;
    public int EggSearch(PlayerEntity master, int eggId) => -1;
    public int SelectEgg(PlayerEntity master, short eggIndex) => -1;
    public int Food(PlayerEntity master) => 0;
    public int HungryVal(PlayerEntity master) => 0;
    public int HungryTimerDelete(PlayerEntity master) => 0;
    public int AttackSkill(PlayerEntity master, EntityId targetId) => 0;
    public int TargetCheck(PlayerEntity master, EntityId targetId, int isType) => 0;
    public void UnlockTarget(PlayerEntity master) { }
    public void Evolution(PlayerEntity master, int evoTo) { }
    public bool EvolutionRequirementsCheck(PlayerEntity master, int evoTo) => false;
    public int BirthProcess(PlayerEntity master) => 0;
    public int ChangeName(PlayerEntity master, string newName) => 0;
    public int ChangeNameAck(PlayerEntity master, byte flag) => 0;
    public int Menu(PlayerEntity master, int choice) => 0;
    public int RecvPetData(PlayerEntity master) => 0;
    public int EquipItem(PlayerEntity master, int inventoryIndex) => 0;
    public int ScCheck(PlayerEntity master, int statusType) => 0;
    public void SetIntimate(PlayerEntity master, int delta) { }
    public void LootItemDrop(PlayerEntity master, int amount) { }
    public void ClearSupportBonuses(PlayerEntity master) { }
    public bool AddAutoBonus(PlayerEntity master, string bonus, int rate, int duration, ushort flag) => false;
    public void DelAutoBonus(PlayerEntity master) { }
    public void ExeAutoBonus(PlayerEntity master) { }
    public void CatchProcessStart(PlayerEntity master, int targetMobClass) { }
    public void CatchProcessEnd(PlayerEntity master, int targetMobClass) { }
    public void Reload() { }
}
