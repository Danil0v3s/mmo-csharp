using Map.Server.Entities;

namespace Map.Server.Items.Db;

/// <summary>
/// Item database queries + per-item rule gates. Canonical entry
/// points for rAthena <c>itemdb.cpp</c> (4 948 lines, 48 public).
///
/// The C# port already has an ItemCatalog covering basic name / id
/// lookups; this service surfaces the rAthena-named gates (`canauction`,
/// `cantrade`, `canmail`, `cancartstore`, …) + the parse facades for
/// the auxiliary YAML loaders (combos, enchant, item groups, package,
/// random options, reform, laphine synthesis / upgrade).
/// </summary>
public interface IItemDbService
{
    // --- per-item gate predicates -------------------------------------
    /// <summary>rAthena <c>itemdb_canauction_sub</c>.</summary>
    bool CanAuction(int itemId, PlayerEntity pc);

    /// <summary>rAthena <c>itemdb_cancartstore_sub</c>.</summary>
    bool CanCartStore(int itemId, PlayerEntity pc);

    /// <summary>rAthena <c>itemdb_canguildstore_sub</c>.</summary>
    bool CanGuildStore(int itemId, PlayerEntity pc);

    /// <summary>rAthena <c>itemdb_canmail_sub</c>.</summary>
    bool CanMail(int itemId, PlayerEntity pc);

    /// <summary>rAthena <c>itemdb_canpartnertrade_sub</c>.</summary>
    bool CanPartnerTrade(int itemId, PlayerEntity pc);

    /// <summary>rAthena <c>itemdb_cansell_sub</c>.</summary>
    bool CanSell(int itemId, PlayerEntity pc);

    /// <summary>rAthena <c>itemdb_canstore_sub</c>.</summary>
    bool CanStore(int itemId, PlayerEntity pc);

    /// <summary>rAthena <c>itemdb_cantrade_sub</c>.</summary>
    bool CanTrade(int itemId, PlayerEntity pc);

    /// <summary>rAthena <c>itemdb_isdropable_sub</c>.</summary>
    bool IsDroppable(int itemId, PlayerEntity pc);

    /// <summary>rAthena <c>itemdb_isrestricted</c>.</summary>
    bool IsRestricted(int itemId, PlayerEntity pc);

    /// <summary>rAthena <c>itemdb_isNoEquip</c>.</summary>
    bool IsNoEquip(int itemId, uint mapId);

    /// <summary>rAthena <c>itemdb_isequip2</c>.</summary>
    bool IsEquip2(int itemId);

    /// <summary>rAthena <c>itemdb_isstackable2</c>.</summary>
    bool IsStackable2(int itemId);

    /// <summary>rAthena <c>itemdb_ishatched_egg</c>.</summary>
    bool IsHatchedEgg(int itemId);

    /// <summary>rAthena <c>itemdb_isidentified</c>.</summary>
    bool IsIdentified(int itemId);

    /// <summary>rAthena <c>itemdb_searchname_array</c>.</summary>
    int SearchNameArray(string namePattern, IList<int> output, int max);

    // --- aux loaders --------------------------------------------------
    /// <summary>rAthena <c>itemdb_reload</c>.</summary>
    void Reload();

    /// <summary>rAthena <c>itemdb_gen_itemmoveinfo</c>.</summary>
    void GenItemMoveInfo();

    /// <summary>rAthena <c>itemdb_parse_roulette_db</c>.</summary>
    bool ParseRouletteDb();

    /// <summary>rAthena <c>ItemGroupDatabase::pc_get_itemgroup</c>.</summary>
    byte GetItemGroup(int groupId, PlayerEntity pc);

    /// <summary>rAthena <c>ComboDatabase::find_combo_id</c>.</summary>
    ushort FindComboId(IReadOnlyList<int> equippedItems);

    /// <summary>rAthena <c>s_random_opt_group::apply</c>.</summary>
    void ApplyRandomOptionGroup(int groupId, IList<(int id, int value, int param)> output);

    /// <summary>rAthena <c>RandomOptionDatabase::option_exists</c>.</summary>
    bool RandomOptionExists(int optionId);

    /// <summary>rAthena <c>RandomOptionDatabase::option_get_id</c>.</summary>
    int RandomOptionGetId(string optionName);
}
