namespace Map.Server.Scripting.Records;

/// <summary>
/// Hooks attached to a <c>registerCombo({...})</c> call. Combos only have
/// a single trigger — <see cref="OnActive"/> — that fires during equip
/// recalc whenever every item in the combo's <c>Members</c> set is
/// equipped on the player.
///
/// <para>
/// Sync invocation only — equip recalc runs on the game loop. The author's
/// closure typically does <c>ctx.bonus(...)</c> / <c>ctx.bonus2(...)</c>
/// calls that accumulate into the active <c>EquipBonusBundle</c>.
/// </para>
/// </summary>
public sealed record ComboHooks(ScriptHandle? OnActive)
{
    // Compiler can't disambiguate `new(null)` between the primary ctor
    // and the synthesized copy ctor; use `default` to force the
    // primary-ctor binding.
    public static readonly ComboHooks Empty = new(default(ScriptHandle));

    public bool Any => OnActive != null;
}
