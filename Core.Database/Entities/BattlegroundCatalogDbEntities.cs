namespace Core.Database.Entities;

/// <summary>
/// Battleground type catalog (rAthena <c>db/battleground_db.yml</c>).
/// One row per BG type (Tierra Gorge, Flavius, KvM, etc.). Job
/// restrictions live in <see cref="BattlegroundJobRestrictionDbEntity"/>;
/// per-map locations in <see cref="BattlegroundLocationDbEntity"/>.
///
/// DB-8f wave replaces the prior <c>BattlegroundDbEntity :
/// PayloadIntKeyEntity</c> JSON blob (which AT-F left for DB-8 to deserialize).
/// Renamed to BattlegroundType* to avoid table-name collision with
/// the AT-D BG queue runtime entities.
/// </summary>
public class BattlegroundTypeDbEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinPlayers { get; set; }
    public int MinLevel { get; set; }
}

/// <summary>
/// Job that is *blocked* from joining a BG type. Composite key
/// (BgId, JobAegis). rAthena yml uses
/// <c>JobRestrictions: { Novice: true, ... }</c>; we flatten to
/// one row per (bg_id, job_aegis).
/// </summary>
public class BattlegroundJobRestrictionDbEntity
{
    public int BgId { get; set; }
    public string JobAegis { get; set; } = string.Empty;
}

/// <summary>
/// One map slot in the BG type's location pool. Composite key
/// (BgId, MapName). When the queue reserves a map, it picks an unused
/// row from here. Each row carries per-team respawn coords + NPC event
/// handles (start / quit / active) bound to NPC scripts on that map.
/// </summary>
public class BattlegroundLocationDbEntity
{
    public int BgId { get; set; }
    public string MapName { get; set; } = string.Empty;
    public string? StartEvent { get; set; }
    // Team A
    public int TeamARespawnX { get; set; }
    public int TeamARespawnY { get; set; }
    public string? TeamAQuitEvent { get; set; }
    public string? TeamAActiveEvent { get; set; }
    public string? TeamAVariable { get; set; }
    // Team B
    public int TeamBRespawnX { get; set; }
    public int TeamBRespawnY { get; set; }
    public string? TeamBQuitEvent { get; set; }
    public string? TeamBActiveEvent { get; set; }
    public string? TeamBVariable { get; set; }
}

/// <summary>
/// Elemental servant catalog (rAthena <c>db/re/elemental_db.yml</c>).
/// One row per elemental class (Agni S/M/L, Aqua S/M/L, etc., 12 base
/// + skill variants). Per-mode skills live in
/// <see cref="ElementalModeDbEntity"/>.
///
/// DB-8f wave replaces the prior <c>ElementalDbEntity :
/// PayloadIntKeyEntity</c> JSON blob.
/// </summary>
public class ElementalCatalogDbEntity
{
    public int Id { get; set; }
    public string AegisName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Size { get; set; } = string.Empty;
    public string Element { get; set; } = string.Empty;
    public int ElementLevel { get; set; }
}

/// <summary>
/// Per-mode skill grant for an elemental. Composite key
/// (ElementalId, Mode). Mode ∈ {Passive, Assist, Aggressive}.
/// </summary>
public class ElementalModeDbEntity
{
    public int ElementalId { get; set; }
    /// <summary>Mode: "Passive" / "Assist" / "Aggressive".</summary>
    public string Mode { get; set; } = string.Empty;
    /// <summary>Skill Aegis name granted under this mode.</summary>
    public string SkillAegis { get; set; } = string.Empty;
}
