using Map.Server.Entities;

namespace Map.Server.Homunculus;

/// <summary>
/// Homunculus lifecycle + evolution + skill tree. Canonical entry
/// points for rAthena <c>homunculus.cpp</c> (2 064 lines, 41
/// functions).
///
/// AI lives in <c>Map.Server.Mob</c> (Homunculus AI rides the same
/// mob AI engine). This service owns the rAthena-named operations
/// + the per-master state mutations.
/// </summary>
public interface IHomunculusService
{
    /// <summary>rAthena <c>hom_call</c> — summon (HLIF_CALLHOMUN).</summary>
    bool Call(PlayerEntity master);

    /// <summary>rAthena <c>hom_create_request</c> — egg incubation request.</summary>
    bool CreateRequest(PlayerEntity master, int classId);

    /// <summary>rAthena <c>hom_recv_data</c> — hydrate from char-server.</summary>
    int RecvData(PlayerEntity master);

    /// <summary>rAthena <c>hom_save</c>.</summary>
    void Save(PlayerEntity master);

    /// <summary>rAthena <c>hom_alloc</c> — allocate homun entity.</summary>
    void Alloc(PlayerEntity master);

    /// <summary>rAthena <c>hom_dead</c>.</summary>
    int Dead(PlayerEntity master);

    /// <summary>rAthena <c>hom_delete</c>.</summary>
    int Delete(PlayerEntity master);

    /// <summary>rAthena <c>hom_ressurect</c>.</summary>
    int Resurrect(PlayerEntity master, byte percent, short x, short y);

    /// <summary>rAthena <c>hom_revive</c>.</summary>
    void Revive(PlayerEntity master);

    /// <summary>rAthena <c>hom_vaporize</c>.</summary>
    int Vaporize(PlayerEntity master, byte flag);

    /// <summary>rAthena <c>hom_evolution</c>.</summary>
    int Evolution(PlayerEntity master);

    /// <summary>rAthena <c>hom_mutate</c>.</summary>
    int Mutate(PlayerEntity master, int newClass);

    /// <summary>rAthena <c>hom_shuffle</c>.</summary>
    int Shuffle(PlayerEntity master);

    /// <summary>rAthena <c>hom_levelup</c>.</summary>
    int LevelUp(PlayerEntity master);

    /// <summary>rAthena <c>hom_gainexp</c>.</summary>
    void GainExp(PlayerEntity master, long amount);

    /// <summary>rAthena <c>hom_heal</c>.</summary>
    void Heal(PlayerEntity master, int hp, int sp);

    /// <summary>rAthena <c>hom_food</c>.</summary>
    int Food(PlayerEntity master);

    /// <summary>rAthena <c>hom_change_name</c>.</summary>
    int ChangeName(PlayerEntity master, string newName);

    /// <summary>rAthena <c>hom_change_name_ack</c>.</summary>
    void ChangeNameAck(PlayerEntity master, byte ok);

    /// <summary>rAthena <c>hom_class2mapid</c>.</summary>
    int Class2MapId(int classId);

    /// <summary>rAthena <c>hom_decrease_intimacy</c>.</summary>
    int DecreaseIntimacy(PlayerEntity master, int delta);

    /// <summary>rAthena <c>hom_increase_intimacy</c>.</summary>
    int IncreaseIntimacy(PlayerEntity master, int delta);

    /// <summary>rAthena <c>hom_get_intimacy_grade</c>.</summary>
    byte GetIntimacyGrade(int intimacy);

    /// <summary>rAthena <c>hom_intimacy_grade2intimacy</c>.</summary>
    uint IntimacyGrade2Intimacy(byte grade);

    /// <summary>rAthena <c>hom_skill_tree_get_max</c>.</summary>
    int SkillTreeGetMax(int classId, ushort skillId);

    /// <summary>rAthena <c>hom_skill_get_min_level</c>.</summary>
    ushort SkillGetMinLevel(ushort skillId);

    /// <summary>rAthena <c>hom_skillup</c>.</summary>
    void SkillUp(PlayerEntity master, ushort skillId);

    /// <summary>rAthena <c>hom_calc_skilltree</c>.</summary>
    void CalcSkillTree(PlayerEntity master);

    /// <summary>rAthena <c>hom_calc_skilltree_sub</c>.</summary>
    void CalcSkillTreeSub(PlayerEntity master);

    /// <summary>rAthena <c>hom_addspiritball</c>.</summary>
    void AddSpiritBall(PlayerEntity master, int max);

    /// <summary>rAthena <c>hom_delspiritball</c>.</summary>
    void DelSpiritBall(PlayerEntity master, int count, bool one);

    /// <summary>rAthena <c>hom_reset_stats</c>.</summary>
    void ResetStats(PlayerEntity master);

    /// <summary>rAthena <c>hom_menu</c>.</summary>
    void Menu(PlayerEntity master, int choice);

    /// <summary>rAthena <c>hom_reload</c>.</summary>
    void Reload();

    /// <summary>rAthena <c>hom_init_timers</c>.</summary>
    void InitTimers(PlayerEntity master);

    /// <summary>rAthena <c>hom_hungry_timer_delete</c>.</summary>
    int HungryTimerDelete(PlayerEntity master);
}
