namespace Core.Database.Entities;

/// <summary>
/// Reputation faction bundle (rAthena
/// <c>db/re/reputation_group.yml</c>). Each group is a named cluster
/// of reputations the client UI shows as one tab (e.g. "Monster
/// Friends" bundles Poring+Lunatic reputations into one row).
///
/// Parent row; child rows live in
/// <see cref="ReputationGroupMemberDbEntity"/>. DB-8a wave re-
/// normalized from <c>PayloadIntKeyEntity</c>.
/// </summary>
public class ReputationGroupDbEntity
{
    /// <summary>Client-side group index (sent in the reputation packet).</summary>
    public int Id { get; set; }

    /// <summary>
    /// rAthena ScriptName field — used by NPC scripts to address the
    /// group by symbolic name (e.g. "MonsterGroup1", "Arunafelts").
    /// </summary>
    public string ScriptName { get; set; } = string.Empty;

    /// <summary>Display name shown in the reputation window tab header.</summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// One reputation id referenced by a <see cref="ReputationGroupDbEntity"/>.
/// Composite key (GroupId, ReputationId). Resolves to a row in the
/// <c>reputation_db</c> table.
/// </summary>
public class ReputationGroupMemberDbEntity
{
    /// <summary>FK to <see cref="ReputationGroupDbEntity.Id"/>.</summary>
    public int GroupId { get; set; }

    /// <summary>
    /// Member reputation id (FK into reputation_db, though no
    /// hard FK constraint — rAthena rows can reference yet-unloaded
    /// reputations as forward declarations).
    /// </summary>
    public int ReputationId { get; set; }
}
