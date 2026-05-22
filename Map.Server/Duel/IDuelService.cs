using Map.Server.Entities;

namespace Map.Server.Duel;

/// <summary>
/// 1v1 duel mode. Canonical entry points for rAthena
/// <c>duel.cpp</c> (311 lines, 11 public functions): invite / accept
/// / reject / leave + check + savetime + showinfo. Duels are
/// transient state (a `duel_id` lives only while two players hold
/// it) — no persistence beyond the duel's lifetime.
///
/// First slice is a registry of active duels keyed by an int id;
/// the wire packets that broadcast invite / accept / participant
/// list land alongside their consumers.
/// </summary>
public interface IDuelService
{
    /// <summary>rAthena <c>duel_create</c> — start a new duel and return its id.</summary>
    int Create(PlayerEntity owner, int? maxMembers = null);

    /// <summary>rAthena <c>duel_invite</c>.</summary>
    bool Invite(PlayerEntity inviter, PlayerEntity invitee);

    /// <summary>rAthena <c>duel_accept</c>.</summary>
    bool Accept(int duelId, PlayerEntity invitee);

    /// <summary>rAthena <c>duel_reject</c>.</summary>
    bool Reject(int duelId, PlayerEntity invitee);

    /// <summary>rAthena <c>duel_leave</c>.</summary>
    bool Leave(int duelId, PlayerEntity who);

    /// <summary>rAthena <c>duel_exist</c>.</summary>
    bool Exists(int duelId);

    /// <summary>rAthena <c>duel_check_player_limit</c>.</summary>
    bool CheckPlayerLimit(int duelId);

    /// <summary>rAthena <c>duel_checktime</c> — daily duel cooldown.</summary>
    bool CheckTime(PlayerEntity who);

    /// <summary>rAthena <c>duel_savetime</c> — record last-duel time for cooldown.</summary>
    void SaveTime(PlayerEntity who);

    /// <summary>rAthena <c>duel_showinfo</c> — list the duel participants.</summary>
    string ShowInfo(int duelId);

    /// <summary>
    /// Scan the active duel registry for one whose membership includes
    /// the given PC. Returns 0 if the PC isn't in any duel. Mirrors
    /// rAthena's <c>sd-&gt;duel_group</c> per-PC pointer; we resolve by
    /// lookup since the C# port doesn't carry the duel id on
    /// PlayerEntity.
    /// </summary>
    int GetDuelIdFor(PlayerEntity pc);
}
