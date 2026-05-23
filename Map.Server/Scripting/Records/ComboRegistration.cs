namespace Map.Server.Scripting.Records;

/// <summary>
/// One <c>registerCombo({ ... })</c> call. A combo activates when the
/// player has every item in <see cref="Members"/> equipped simultaneously.
///
/// <para>
/// <see cref="Members"/> stores item <em>aegis names</em> (rAthena
/// <c>name_aegis</c> column, e.g. "Goibne's_Armor") matching the
/// <c>item_combo_member_db.member_item_aegis</c> column. The dispatcher
/// resolves names to numeric ids via IItemCatalog at boot.
/// </para>
///
/// <para>
/// The original combo_id from rAthena's <c>item_combo_db</c> is preserved
/// in <see cref="ComboId"/> for traceability — the converter emits it as
/// a comment + sets the field so log lines can cite "combo #1234" back
/// to the seed row.
/// </para>
/// </summary>
public sealed record ComboRegistration
{
    public required int ComboId { get; init; }
    public required IReadOnlyList<string> Members { get; init; }
    public required ComboHooks Hooks { get; init; }
}
