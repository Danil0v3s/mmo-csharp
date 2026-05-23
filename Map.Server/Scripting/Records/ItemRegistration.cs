namespace Map.Server.Scripting.Records;

/// <summary>
/// One <c>registerItem({ ... })</c> call, marshalled into a typed record.
/// The TS-side shape lives in <c>scripts/types/api.d.ts</c>; this record
/// is the C# mirror.
///
/// <para>
/// Items are keyed by numeric <c>Id</c> (rAthena <c>id</c> column).
/// <c>NameAegis</c> is the script-side identifier (e.g. "Red_Potion")
/// that combos cite as a member; <c>NameEnglish</c> is the display name.
/// All three come from <c>item_db</c>. The <see cref="Hooks"/> bundle
/// carries any of OnUse / OnEquip / OnUnequip the author defined.
/// </para>
///
/// <para>
/// This record only captures the *scriptable* surface — the static stats
/// (type, weight, slots, jobs, etc.) stay in SQL / IItemCatalog. Authors
/// who want to override stats can do so through the existing catalog
/// service; the registrar is purely for hook attachment.
/// </para>
/// </summary>
public sealed record ItemRegistration
{
    public required int Id { get; init; }
    public required string NameAegis { get; init; }
    public string? NameEnglish { get; init; }
    public required ItemHooks Hooks { get; init; }
}
