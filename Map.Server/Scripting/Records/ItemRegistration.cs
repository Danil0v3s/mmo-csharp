namespace Map.Server.Scripting.Records;

/// <summary>
/// One <c>registerItem({ ... })</c> call, marshalled into a typed record.
/// The TS-side shape lives in <c>scripts/types/api.d.ts</c>; this record
/// is the C# mirror.
///
/// <para>
/// Items are keyed by numeric <c>Id</c> (rAthena <c>id</c> column) only —
/// every other item-db column (name_aegis, name_english, type, weight,
/// slots, jobs, …) already lives in SQL / <c>IItemCatalog</c>, so the
/// registrar is purely for hook attachment by id. Combo members cite
/// aegis names, which get resolved to ids via the catalog at dispatch
/// time; they don't need to be re-declared on each item registration.
/// </para>
/// </summary>
public sealed record ItemRegistration
{
    public required int Id { get; init; }
    public required ItemHooks Hooks { get; init; }
}
