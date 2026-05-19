using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// rAthena marriage / adoption family — <c>pc_marriage</c>,
/// <c>pc_divorce</c>, <c>pc_adoption</c>, <c>pc_try_adopt</c>
/// (pc.cpp:1037-1170). CharEntity already exposes partner_id /
/// father / mother / child fields so the persistence side is in
/// place; this service centralises the validation + IPC notify the
/// other party.
/// </summary>
public interface IPlayerRelationService
{
    /// <summary>
    /// rAthena <c>pc_marriage</c> — link two PCs as partners. Both
    /// must be online + opposite-sex (rAthena
    /// <c>battle_config.wedding_modifydisplay</c> aside) + not already
    /// married. Returns false on validation failure.
    /// </summary>
    bool Marry(PlayerEntity a, PlayerEntity b);

    /// <summary>
    /// rAthena <c>pc_divorce</c> — break a marriage. Both parties'
    /// partner_id is cleared; wedding rings get destroyed.
    /// </summary>
    bool Divorce(PlayerEntity pc);

    /// <summary>
    /// rAthena <c>pc_try_adopt</c> — pre-checks for an adoption
    /// (parents married, baby novice level cap, baby not already
    /// adopted). Returns the rAthena <c>adopt_responses</c> code.
    /// </summary>
    AdoptResult TryAdopt(PlayerEntity parent1, PlayerEntity parent2, PlayerEntity baby);

    /// <summary>
    /// rAthena <c>pc_adoption</c> — applies an adoption: baby class
    /// flips to Baby* variant, father/mother/child fields wire up.
    /// </summary>
    bool Adopt(PlayerEntity parent1, PlayerEntity parent2, PlayerEntity baby);
}

/// <summary>rAthena <c>enum adopt_responses</c> (pc.hpp).</summary>
public enum AdoptResult
{
    Allowed,
    AlreadyAdopted,
    Married,
    LevelTooHigh,
    NotNovice,
    CharacterNotFound,
}
