using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Status;

namespace Map.Server.Inventory.Script;

/// <summary>
/// V8-callable host object exposed to translated rAthena item scripts.
/// One instance per <see cref="IScriptedBonusService.Apply"/> call —
/// holds the per-PC context (bundle, equipped items, autobonus
/// service) the script reads and writes.
///
/// <para>
/// Every method matches a function in the rAthena item-script DSL.
/// Method names are the verbatim rAthena names (lowercase) so the
/// translator's <c>h.&lt;name&gt;(...)</c> emission is direct.
/// ClearScript binds JS positional args to C# params automatically.
/// </para>
///
/// <para>
/// Public surface — must stay JS-callable. Mark anything internal
/// or per-call mutable state private to avoid leaking host fields
/// into the JS namespace.
/// </para>
/// </summary>
public sealed class ScriptedBonusHost
{
    private readonly PlayerEntity _pc;
    private readonly EquipBonusBundle _bundle;
    private readonly IReadOnlyList<InventoryItem>? _equipped;
    private readonly IItemCatalog? _catalog;
    private readonly IPlayerBonusService? _bonusSvc;

    public ScriptedBonusHost(
        PlayerEntity pc,
        EquipBonusBundle bundle,
        IReadOnlyList<InventoryItem>? equipped = null,
        IItemCatalog? catalog = null,
        IPlayerBonusService? bonusSvc = null)
    {
        _pc = pc;
        _bundle = bundle;
        _equipped = equipped;
        _catalog = catalog;
        _bonusSvc = bonusSvc;
    }

    // ----- bonus / bonus2 / bonus3 / bonus4 / bonus5 -----

    /// <summary>rAthena <c>bonus bKey, val;</c> — flat stat bump.</summary>
    public void bonus(string key, int value)
    {
        // Tokenizer strips the leading 'b' from the dispatch table
        // (BonusScriptExtractor.ApplyFlat); we keep the 'b'-stripping
        // there so both paths share the same case-insensitive switch.
        var stripped = StripBPrefix(key);
        BonusScriptExtractor.ApplyFlatBonus(_bundle, stripped, value);
    }

    /// <summary>rAthena <c>bonus2 bKey, idx, val;</c> — race/element/size/class indexed.</summary>
    public void bonus2(string key, object idxToken, int value)
    {
        var stripped = StripBPrefix(key);
        BonusScriptExtractor.ApplyIndexedBonus(_bundle, stripped, idxToken?.ToString() ?? "", value);
    }

    /// <summary>
    /// rAthena <c>bonus3 bKey, a, b, val;</c>. Most bonus3 in stock
    /// item_combos are <c>bAutoSpell</c> / <c>bAutoSpellWhenHit</c>
    /// (handled via the autobonus registry below); a handful of
    /// flat bonus3 patterns are silently skipped — the regex extractor
    /// already covers the major flat patterns, and this DSL path
    /// inherits the same coverage.
    /// </summary>
    public void bonus3(string key, object a, object b, int value)
    {
        var stripped = StripBPrefix(key);
        // bonus3 bAutoSpell, "SkillName", lv, rate — register as
        // an OnHit autobonus that fires the skill at <rate>/10000.
        // The script body is a synthetic "skill" call.
        if (string.Equals(stripped, "AutoSpell", StringComparison.OrdinalIgnoreCase))
        {
            var skillName = a?.ToString() ?? "";
            var script = $"skill \"{skillName}\",{value};";
            _bonusSvc?.AddAutobonus(_pc, AutobonusTrigger.OnHit, script,
                rate: value, durationMs: 0, flag: 0);
            return;
        }
        if (string.Equals(stripped, "AutoSpellWhenHit", StringComparison.OrdinalIgnoreCase))
        {
            var skillName = a?.ToString() ?? "";
            var script = $"skill \"{skillName}\",{value};";
            _bonusSvc?.AddAutobonus(_pc, AutobonusTrigger.WhenHit, script,
                rate: value, durationMs: 0, flag: 0);
            return;
        }
        // Other bonus3 patterns are no-ops in the regex extractor too;
        // documenting parity: skip silently.
    }

    /// <summary>rAthena <c>bonus4 bKey, a, b, c, val;</c>. No-op for now (most are autocast).</summary>
    public void bonus4(string key, object a, object b, object c, int value)
    {
        // bonus4 bAutoSpellOnSkill, "Source", "Spell", lv, rate
        var stripped = StripBPrefix(key);
        if (string.Equals(stripped, "AutoSpellOnSkill", StringComparison.OrdinalIgnoreCase))
        {
            var triggerSkill = a?.ToString() ?? "";
            var spell = b?.ToString() ?? "";
            var script = $"skill \"{spell}\",{c};";
            _bonusSvc?.AddAutobonus(_pc, AutobonusTrigger.OnSkill, script,
                rate: value, durationMs: 0, flag: 0);
        }
    }

    /// <summary>rAthena <c>bonus5</c>. Unhandled — no stock combo uses bonus5 in a parseable way.</summary>
    public void bonus5(string key, object a, object b, object c, object d, int value) { }

    // ----- autobonus family -----

    /// <summary>
    /// rAthena <c>autobonus "{body}", rate, duration, atkType;</c>.
    /// Registers an OnHit autobonus whose wrapped script body fires
    /// (rate/10000) on weapon attack landing.
    /// </summary>
    public void autobonus(string body, int rate, int durationMs, object flag)
    {
        _bonusSvc?.AddAutobonus(_pc, AutobonusTrigger.OnHit, body,
            rate, durationMs, ParseAtkType(flag));
    }

    /// <summary>
    /// rAthena <c>autobonus2 "{body}", rate, duration, atkType;</c>.
    /// Triggers when the PC takes damage (WhenHit).
    /// </summary>
    public void autobonus2(string body, int rate, int durationMs, object flag)
    {
        _bonusSvc?.AddAutobonus(_pc, AutobonusTrigger.WhenHit, body,
            rate, durationMs, ParseAtkType(flag));
    }

    /// <summary>
    /// rAthena <c>autobonus3 "{body}", rate, duration, "SkillName";</c>.
    /// Triggers when the PC casts the named skill.
    /// </summary>
    public void autobonus3(string body, int rate, int durationMs, object skillName)
    {
        _bonusSvc?.AddAutobonus(_pc, AutobonusTrigger.OnSkill, body,
            rate, durationMs, flag: 0);
    }

    // ----- SC / skill family -----

    /// <summary>
    /// rAthena <c>sc_start SC, duration, val1;</c>. Apply a status
    /// change with the given duration. Routes through the SC engine
    /// when it's wired into the host (today: log-only).
    /// </summary>
    public void sc_start(object sc, int durationMs, int val1) { /* SC engine wire-up data-pending */ }

    /// <summary>rAthena <c>skill SkillName, level, [type];</c> — grant a skill.</summary>
    public void skill(object skillName, int level) { /* IPlayerSkillService.Grant data-pending wire-up */ }

    // ----- expression helpers -----

    /// <summary>
    /// rAthena <c>getrefine()</c> — refine of the item triggering the
    /// script. For combo / item bonus scripts the convention is to use
    /// the weapon refine; until per-item context plumbs through, we
    /// default to the right-hand weapon's refine.
    /// </summary>
    public int getrefine() => GetRefineForSlot("EQI_HAND_R");

    /// <summary>
    /// rAthena <c>getequiprefinerycnt(EQI_SLOT)</c> — refine of the
    /// item in the given equip slot. Returns 0 if no item is equipped
    /// there or the slot token is unknown.
    /// </summary>
    public int getequiprefinerycnt(string slotToken) => GetRefineForSlot(slotToken);

    /// <summary>rAthena <c>getskilllv("SkillName")</c> — caller's learned level (0 if unknown).</summary>
    public int getskilllv(string skillName) => 0; // IPlayerSkillService wire-up data-pending

    /// <summary>rAthena <c>max(a, b)</c>.</summary>
    public int max(int a, int b) => Math.Max(a, b);
    /// <summary>rAthena <c>min(a, b)</c>.</summary>
    public int min(int a, int b) => Math.Min(a, b);
    /// <summary>rAthena <c>pow(a, b)</c>.</summary>
    public int pow(int a, int b) => (int)Math.Pow(a, b);
    /// <summary>rAthena <c>rand(n)</c> — uniform [0, n).</summary>
    public int rand(int n) => Random.Shared.Next(n);

    /// <summary>
    /// PC parameter read (rAthena <c>pc_readparam</c>). Translator
    /// emits <c>h.getParam("Class")</c> etc. for the whitelisted
    /// param names.
    /// </summary>
    public int getParam(string name) => name switch
    {
        "BaseLevel"     => _pc.Level,
        "JobLevel"      => _pc.JobLevel,
        "Hp"            => _pc.Hp,
        "MaxHp"         => _pc.MaxHp,
        "Sp"            => _pc.Sp,
        "MaxSp"         => _pc.MaxSp,
        "StatusPoint"   => _pc.StatusPoints,
        "SkillPoint"    => _pc.SkillPoints,
        "TraitPoint"    => _pc.TraitPoints,
        "Karma"         => _pc.Karma,
        "Cash"          => _pc.CashPoints,
        "KafraPoints"   => _pc.KafraPoints,
        "PartyId"       => _pc.PartyId,
        "GuildId"       => _pc.GuildId,
        "ClanId"        => _pc.ClanId,
        "Str"           => _pc.Stats.Str,
        "Agi"           => _pc.Stats.Agi,
        "Vit"           => _pc.Stats.Vit,
        "Int"           => _pc.Stats.IntStat,
        "Dex"           => _pc.Stats.Dex,
        "Luk"           => _pc.Stats.Luk,
        "Pow"           => _pc.Stats.Pow,
        "Sta"           => _pc.Stats.Sta,
        "Wis"           => _pc.Stats.Wis,
        "Spl"           => _pc.Stats.Spl,
        "Con"           => _pc.Stats.Con,
        "Crt"           => _pc.Stats.Crt,
        // Class / Sex / Zeny live on MapSessionData / CharEntity, not
        // PlayerEntity — return 0 here until the resolver plumbs through.
        // Combo scripts that gate on Class == 4008 etc. will silently
        // skip until then, which is the safer-bias.
        "Class"         => 0,
        "Sex"           => 0,
        "Zeny"          => 0,
        _               => 0,
    };

    // ----- private helpers -----

    /// <summary>
    /// rAthena bonus keys are conventionally prefixed with 'b'
    /// (bAtk, bMatk, bAddRace). The existing
    /// <see cref="BonusScriptExtractor"/> switch table also strips it.
    /// </summary>
    private static string StripBPrefix(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        if (key.Length > 1 && (key[0] == 'b' || key[0] == 'B'))
            return key[1..];
        return key;
    }

    /// <summary>
    /// Map EQI_* slot tokens onto the EquipBits used by
    /// <see cref="InventoryItem.Equip"/>. Mirrors rAthena
    /// <c>enum equip_index</c> (pc.hpp).
    /// </summary>
    private int GetRefineForSlot(string slotToken)
    {
        if (_equipped == null) return 0;
        var bits = slotToken switch
        {
            "EQI_HEAD_TOP"      => EquipBonusAggregator.EquipHelm,
            "EQI_HAND_R"        => EquipBonusAggregator.EquipRightHand,
            "EQI_HAND_L"        => EquipBonusAggregator.EquipLeftHand,
            "EQI_ARMOR"         => EquipBonusAggregator.EquipArmor,
            "EQI_GARMENT"       => EquipBonusAggregator.EquipGarment,
            "EQI_SHOES"         => EquipBonusAggregator.EquipShoes,
            "EQI_ACC_R"         => EquipBonusAggregator.EquipAccessoryR,
            "EQI_ACC_L"         => EquipBonusAggregator.EquipAccessoryL,
            // Costume / shadow / head-mid / head-low fall through to 0
            // until the EquipBonusAggregator surface adds them.
            _ => 0u,
        };
        if (bits == 0) return 0;
        for (var i = 0; i < _equipped.Count; i++)
        {
            var item = _equipped[i];
            if ((item.Equip & bits) != 0) return item.Refine;
        }
        return 0;
    }

    /// <summary>
    /// Coerce a JS arg to the integer atk-type flag used by
    /// <see cref="IPlayerBonusService.AddAutobonus"/>. Strings like
    /// "BF_WEAPON" / "BF_MAGIC" map onto the standard rAthena BF_*
    /// numeric flags; numeric args pass through.
    /// </summary>
    private static ushort ParseAtkType(object flag)
    {
        if (flag is int i) return (ushort)i;
        if (flag is double d) return (ushort)d;
        var s = flag?.ToString() ?? "";
        return s switch
        {
            "BF_WEAPON" => 1,
            "BF_MAGIC"  => 2,
            "BF_MISC"   => 4,
            "BF_NORMAL" => 0x10,
            "BF_SKILL"  => 0x20,
            "BF_LONG"   => 0x40,
            "BF_SHORT"  => 0x80,
            _           => 0,
        };
    }
}
