using Map.Server.Entities;

namespace Map.Server.Pet.PetOps;

/// <summary>
/// rAthena <c>pet.cpp</c> public surface (2 504 lines, 33 functions).
/// The pet AI + lifecycle already live in <c>Map.Server.Pet</c>; this
/// service surfaces the rAthena-named entry points (catch, evolve,
/// equip, attack-skill, intimacy, autobonus) so callers don't drift.
/// </summary>
public interface IPetOpsService
{
    /// <summary>rAthena <c>pet_data_init</c>.</summary>
    bool DataInit(PlayerEntity master, byte flag);
    /// <summary>rAthena <c>pet_create_egg</c>.</summary>
    bool CreateEgg(PlayerEntity master, int itemId);
    /// <summary>rAthena <c>pet_get_egg</c>.</summary>
    bool GetEgg(PlayerEntity master, int classId, int itemId, byte gender);
    /// <summary>rAthena <c>pet_return_egg</c>.</summary>
    bool ReturnEgg(PlayerEntity master);
    /// <summary>rAthena <c>pet_egg_search</c>.</summary>
    int EggSearch(PlayerEntity master, int eggId);
    /// <summary>rAthena <c>pet_select_egg</c>.</summary>
    int SelectEgg(PlayerEntity master, short eggIndex);
    /// <summary>rAthena <c>pet_food</c>.</summary>
    int Food(PlayerEntity master);
    /// <summary>rAthena <c>pet_hungry_val</c>.</summary>
    int HungryVal(PlayerEntity master);
    /// <summary>rAthena <c>pet_hungry_timer_delete</c>.</summary>
    int HungryTimerDelete(PlayerEntity master);
    /// <summary>rAthena <c>pet_attackskill</c>.</summary>
    int AttackSkill(PlayerEntity master, EntityId targetId);
    /// <summary>rAthena <c>pet_target_check</c>.</summary>
    int TargetCheck(PlayerEntity master, EntityId targetId, int isType);
    /// <summary>rAthena <c>pet_unlocktarget</c>.</summary>
    void UnlockTarget(PlayerEntity master);
    /// <summary>rAthena <c>pet_evolution</c>.</summary>
    void Evolution(PlayerEntity master, int evoTo);
    /// <summary>rAthena <c>pet_evolution_requirements_check</c>.</summary>
    bool EvolutionRequirementsCheck(PlayerEntity master, int evoTo);
    /// <summary>rAthena <c>pet_birth_process</c>.</summary>
    int BirthProcess(PlayerEntity master);
    /// <summary>rAthena <c>pet_change_name</c>.</summary>
    int ChangeName(PlayerEntity master, string newName);
    /// <summary>rAthena <c>pet_change_name_ack</c>.</summary>
    int ChangeNameAck(PlayerEntity master, byte flag);
    /// <summary>rAthena <c>pet_menu</c>.</summary>
    int Menu(PlayerEntity master, int choice);
    /// <summary>rAthena <c>pet_recv_petdata</c>.</summary>
    int RecvPetData(PlayerEntity master);
    /// <summary>rAthena <c>pet_equipitem</c>.</summary>
    int EquipItem(PlayerEntity master, int inventoryIndex);
    /// <summary>rAthena <c>pet_sc_check</c>.</summary>
    int ScCheck(PlayerEntity master, int statusType);
    /// <summary>rAthena <c>pet_set_intimate</c>.</summary>
    void SetIntimate(PlayerEntity master, int delta);
    /// <summary>rAthena <c>pet_lootitem_drop</c>.</summary>
    void LootItemDrop(PlayerEntity master, int amount);
    /// <summary>rAthena <c>pet_clear_support_bonuses</c>.</summary>
    void ClearSupportBonuses(PlayerEntity master);
    /// <summary>rAthena <c>pet_addautobonus</c>.</summary>
    bool AddAutoBonus(PlayerEntity master, string bonus, int rate, int duration, ushort flag);
    /// <summary>rAthena <c>pet_delautobonus</c>.</summary>
    void DelAutoBonus(PlayerEntity master);
    /// <summary>rAthena <c>pet_exeautobonus</c>.</summary>
    void ExeAutoBonus(PlayerEntity master);
    /// <summary>rAthena <c>pet_catch_process_start</c>.</summary>
    void CatchProcessStart(PlayerEntity master, int targetMobClass);
    /// <summary>rAthena <c>pet_catch_process_end</c>.</summary>
    void CatchProcessEnd(PlayerEntity master, int targetMobClass);
    /// <summary>rAthena <c>PetDatabase::reload</c>.</summary>
    void Reload();
}
