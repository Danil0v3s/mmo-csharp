namespace Tools.RathenaImporter;

/// <summary>
/// Per-file YAML → SQL converter. Reads one rAthena YAML database
/// from <c>db/re/</c> (or <c>db/</c>) and emits a single
/// <c>seed_*_db.sql</c> file under
/// <c>Core.Database/Seeds/Scripts/</c>.
///
/// One converter per <c>_db</c> table; the program enumerates the
/// registry and runs every converter in sequence. New tables get
/// added here by implementing the interface — no other plumbing.
/// </summary>
public interface IYamlToSqlConverter
{
    /// <summary>
    /// Friendly name shown in console output (e.g. "skill_db").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// rAthena YAML source path relative to the rAthena repo root.
    /// Example: <c>db/re/skill_db.yml</c>.
    /// </summary>
    string SourceYamlPath { get; }

    /// <summary>
    /// Output SQL path relative to the C# repo root.
    /// Example: <c>Core.Database/Seeds/Scripts/seed_skill_db.sql</c>.
    /// </summary>
    string OutputSqlPath { get; }

    /// <summary>Reads the YAML, returns SQL text. Empty string skips the write.</summary>
    Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default);
}
