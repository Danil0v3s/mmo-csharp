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

    /// <summary>
    /// Exposed as <c>ctx.player</c> on the TypeScript surface (lowercase
    /// name so the JS literal matches api.d.ts <c>ItemEquipContext.player</c>).
    /// Generated combo scripts don't reach for this — they go through
    /// <c>ctx.getParam</c> / <c>ctx.readparam</c> — but hand-written items
    /// touch it for things like <c>ctx.player.hp = 1</c> on unequip.
    /// </summary>
    public PlayerEntity player => _pc;

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
    // All take params object[] so V8 can dispatch any arity. rAthena's
    // bonus family is genuinely variadic — there's a 1-arg flag form
    // (`bonus bNoCastCancel;`), a 2-arg numeric form, a 2-arg indexed
    // form, and bigger variants for autospell / autobonus-on-skill.
    // Arity-based dispatch inside each method picks the right path.

    /// <summary>rAthena <c>bonus bKey[, val];</c>.</summary>
    public void bonus(params object[] args)
    {
        if (args.Length == 0) return;
        var key = StripBPrefix(args[0]?.ToString() ?? "");
        if (args.Length == 1)
        {
            // Flag bonus (e.g. bonus bNoCastCancel;). The regex
            // extractor doesn't handle this form either — silently
            // skip for now; specific flags wire in as needed.
            return;
        }
        var value = ToInt(args[1]);
        BonusScriptExtractor.ApplyFlatBonus(_bundle, key, value);
    }

    /// <summary>rAthena <c>bonus2 bKey, idx, val;</c>.</summary>
    public void bonus2(params object[] args)
    {
        if (args.Length < 3) return;
        var key = StripBPrefix(args[0]?.ToString() ?? "");
        var idx = args[1]?.ToString() ?? "";
        var value = ToInt(args[2]);
        BonusScriptExtractor.ApplyIndexedBonus(_bundle, key, idx, value);
    }

    /// <summary>
    /// rAthena <c>bonus3 bKey, a, b, val;</c>. Most bonus3 in stock
    /// item_combos are <c>bAutoSpell</c> / <c>bAutoSpellWhenHit</c>
    /// (registered as autobonus entries); a handful of flat bonus3
    /// patterns are silently skipped — the regex extractor covers the
    /// major flat patterns and this DSL path inherits the same
    /// coverage gap.
    /// </summary>
    public void bonus3(params object[] args)
    {
        if (args.Length < 4) return;
        var key = StripBPrefix(args[0]?.ToString() ?? "");
        if (string.Equals(key, "AutoSpell", StringComparison.OrdinalIgnoreCase))
        {
            var skillName = args[1]?.ToString() ?? "";
            var lvl = ToInt(args[2]);
            var rate = ToInt(args[3]);
            var script = $"skill \"{skillName}\",{lvl};";
            _bonusSvc?.AddAutobonus(_pc, AutobonusTrigger.OnHit, script,
                rate: rate, durationMs: 0, flag: 0);
            return;
        }
        if (string.Equals(key, "AutoSpellWhenHit", StringComparison.OrdinalIgnoreCase))
        {
            var skillName = args[1]?.ToString() ?? "";
            var lvl = ToInt(args[2]);
            var rate = ToInt(args[3]);
            var script = $"skill \"{skillName}\",{lvl};";
            _bonusSvc?.AddAutobonus(_pc, AutobonusTrigger.WhenHit, script,
                rate: rate, durationMs: 0, flag: 0);
        }
    }

    /// <summary>rAthena <c>bonus4 bKey, a, b, c, val;</c>.</summary>
    public void bonus4(params object[] args)
    {
        if (args.Length < 5) return;
        var key = StripBPrefix(args[0]?.ToString() ?? "");
        // bonus4 bAutoSpellOnSkill, "Source", "Spell", lv, rate
        if (string.Equals(key, "AutoSpellOnSkill", StringComparison.OrdinalIgnoreCase))
        {
            var spell = args[2]?.ToString() ?? "";
            var lvl = ToInt(args[3]);
            var rate = ToInt(args[4]);
            var script = $"skill \"{spell}\",{lvl};";
            _bonusSvc?.AddAutobonus(_pc, AutobonusTrigger.OnSkill, script,
                rate: rate, durationMs: 0, flag: 0);
        }
    }

    /// <summary>rAthena <c>bonus5</c>. Variants are rare in item scripts; no-op for parity with the regex path.</summary>
    public void bonus5(params object[] args) { }

    /// <summary>
    /// Defensive int coercion — V8 numbers arrive as double; ClearScript
    /// auto-converts but rare edge cases (strings like "1") need an
    /// explicit pass.
    /// </summary>
    private static int ToInt(object? o)
    {
        if (o == null) return 0;
        if (o is int i) return i;
        if (o is long l) return (int)l;
        if (o is double d) return (int)d;
        if (o is float f) return (int)f;
        return int.TryParse(o.ToString(), out var n) ? n : 0;
    }

    // ----- autobonus family -----
    // All three variadic — autobonus / autobonus2 take an optional
    // 5th-arg "on-fail" script, autobonus3 takes a skill name string.

    /// <summary>rAthena <c>autobonus "{body}", rate, duration, [atkType], [onfailScript];</c>.</summary>
    public void autobonus(params object[] args)
    {
        if (args.Length < 3) return;
        var body = args[0]?.ToString() ?? "";
        var rate = ToInt(args[1]);
        var dur = ToInt(args[2]);
        var flag = args.Length > 3 ? ParseAtkType(args[3]) : (ushort)0;
        _bonusSvc?.AddAutobonus(_pc, AutobonusTrigger.OnHit, body, rate, dur, flag);
    }

    /// <summary>rAthena <c>autobonus2 "{body}", rate, duration, [atkType], [onfailScript];</c>.</summary>
    public void autobonus2(params object[] args)
    {
        if (args.Length < 3) return;
        var body = args[0]?.ToString() ?? "";
        var rate = ToInt(args[1]);
        var dur = ToInt(args[2]);
        var flag = args.Length > 3 ? ParseAtkType(args[3]) : (ushort)0;
        _bonusSvc?.AddAutobonus(_pc, AutobonusTrigger.WhenHit, body, rate, dur, flag);
    }

    /// <summary>rAthena <c>autobonus3 "{body}", rate, duration, "SkillName", [onfailScript];</c>.</summary>
    public void autobonus3(params object[] args)
    {
        if (args.Length < 3) return;
        var body = args[0]?.ToString() ?? "";
        var rate = ToInt(args[1]);
        var dur = ToInt(args[2]);
        _bonusSvc?.AddAutobonus(_pc, AutobonusTrigger.OnSkill, body, rate, dur, flag: 0);
    }

    // ----- SC / skill family -----
    // Variadic — rAthena overloads vary: sc_start SC,dur,val1 or
    // sc_start2 SC,dur,val1,rate or sc_start4 SC,dur,val1,val2,val3,val4
    // plus skill_id forms. We log-only here until consumer services wire in.

    /// <summary>rAthena <c>sc_start SC, duration, val1, [rate];</c>.</summary>
    public void sc_start(params object[] args) { /* SC engine wire-up data-pending */ }
    /// <summary>rAthena <c>sc_start2 SC, duration, val1, rate;</c>.</summary>
    public void sc_start2(params object[] args) { /* same as sc_start */ }
    /// <summary>rAthena <c>sc_start4 SC, duration, val1, val2, val3, val4;</c>.</summary>
    public void sc_start4(params object[] args) { /* same as sc_start */ }
    /// <summary>rAthena <c>sc_end SC;</c>.</summary>
    public void sc_end(params object[] args) { /* same as sc_start */ }

    /// <summary>rAthena <c>skill SkillName, level, [type];</c>.</summary>
    public void skill(params object[] args) { /* IPlayerSkillService.Grant wire-up data-pending */ }

    /// <summary>rAthena <c>heal hp, sp;</c>.</summary>
    public void heal(params object[] args) { /* data-pending */ }
    /// <summary>rAthena <c>percentheal hp%, sp%;</c>.</summary>
    public void percentheal(params object[] args) { /* data-pending */ }
    /// <summary>rAthena <c>itemheal hp, sp;</c>.</summary>
    public void itemheal(params object[] args) { /* data-pending */ }
    /// <summary>rAthena <c>specialeffect effectId;</c>.</summary>
    public void specialeffect(params object[] args) { /* visual-only */ }
    /// <summary>rAthena <c>specialeffect2 effectId;</c>.</summary>
    public void specialeffect2(params object[] args) { /* visual-only */ }
    /// <summary>rAthena <c>hateffect effectId, state;</c>.</summary>
    public void hateffect(params object[] args) { /* cosmetic */ }
    /// <summary>rAthena <c>petloot count;</c>.</summary>
    public void petloot(params object[] args) { /* pet AI data-pending */ }
    /// <summary>rAthena <c>setoption flag, [value];</c>.</summary>
    public void setoption(params object[] args) { /* state mod */ }
    /// <summary>rAthena <c>message "..."</c>.</summary>
    public void message(params object[] args) { /* UI message */ }
    /// <summary>rAthena <c>dispbottom "..."</c>.</summary>
    public void dispbottom(params object[] args) { /* UI message */ }

    // ----- expression helpers -----
    // All variadic; the few cases that need a fixed-arity invariant
    // (max/min/pow) check inside. JS-side calls fall through cleanly
    // even when V8 hands us floating-point numbers instead of ints.

    /// <summary>rAthena <c>getrefine()</c>.</summary>
    public int getrefine(params object[] _) => GetRefineForSlot("EQI_HAND_R");
    /// <summary>rAthena <c>getequiprefinerycnt(EQI_SLOT)</c>.</summary>
    public int getequiprefinerycnt(params object[] args)
        => args.Length == 0 ? 0 : GetRefineForSlot(args[0]?.ToString() ?? "");
    /// <summary>rAthena <c>getskilllv("SkillName")</c>. Returns 0 until skill service wires in.</summary>
    public int getskilllv(params object[] _) => 0;
    /// <summary>rAthena <c>getequipid(EQI_SLOT)</c>.</summary>
    public int getequipid(params object[] _) => 0;
    /// <summary>
    /// rAthena <c>eaclass()</c> — returns a bitmask describing the PC's
    /// expanded job class (EAJL_THIRD, EAJL_BABY, etc.). Returns 0 until
    /// the class resolver wires in; conditional combos gated on
    /// <c>eaclass() &amp; EAJL_THIRD</c> will silently skip — safer than
    /// firing on the wrong class.
    /// </summary>
    public int eaclass(params object[] _) => 0;
    /// <summary>rAthena <c>readparam(SP_X)</c> — synonym for getParam.</summary>
    public int readparam(params object[] args)
        => args.Length == 0 ? 0 : getParam(args[0]?.ToString() ?? "");
    /// <summary>rAthena <c>getiteminfo(itemId, n)</c>.</summary>
    public int getiteminfo(params object[] _) => 0;
    /// <summary>rAthena <c>getitemcount(itemId)</c>.</summary>
    public int getitemcount(params object[] _) => 0;
    /// <summary>rAthena <c>checkoption(opt)</c>.</summary>
    public int checkoption(params object[] _) => 0;
    /// <summary>rAthena <c>checkmount()</c>.</summary>
    public int checkmount(params object[] _) => 0;
    /// <summary>rAthena <c>countitem(itemId)</c>.</summary>
    public int countitem(params object[] _) => 0;
    /// <summary>rAthena <c>isequipped(itemId, ...)</c>.</summary>
    public int isequipped(params object[] _) => 0;
    /// <summary>rAthena <c>isequippedcnt(itemId, ...)</c>.</summary>
    public int isequippedcnt(params object[] _) => 0;
    /// <summary>rAthena <c>basicskillcheck()</c>.</summary>
    public int basicskillcheck(params object[] _) => 1;
    /// <summary>rAthena <c>checkfalcon()</c>.</summary>
    public int checkfalcon(params object[] _) => 0;
    /// <summary>rAthena <c>checkriding()</c>.</summary>
    public int checkriding(params object[] _) => 0;
    /// <summary>rAthena <c>checkcart()</c>.</summary>
    public int checkcart(params object[] _) => 0;
    /// <summary>rAthena <c>checkidle()</c>.</summary>
    public int checkidle(params object[] _) => 0;

    /// <summary>rAthena <c>max(a, b)</c>.</summary>
    public int max(params object[] args)
    {
        if (args.Length == 0) return 0;
        var n = ToInt(args[0]);
        for (var i = 1; i < args.Length; i++) n = Math.Max(n, ToInt(args[i]));
        return n;
    }
    /// <summary>rAthena <c>min(a, b)</c>.</summary>
    public int min(params object[] args)
    {
        if (args.Length == 0) return 0;
        var n = ToInt(args[0]);
        for (var i = 1; i < args.Length; i++) n = Math.Min(n, ToInt(args[i]));
        return n;
    }
    /// <summary>rAthena <c>pow(a, b)</c>.</summary>
    public int pow(params object[] args)
        => args.Length < 2 ? 0 : (int)Math.Pow(ToInt(args[0]), ToInt(args[1]));
    /// <summary>rAthena <c>rand(n)</c> — uniform [0, n).</summary>
    public int rand(params object[] args)
        => args.Length == 0 ? 0 : Random.Shared.Next(Math.Max(1, ToInt(args[0])));

    /// <summary>
    /// PC parameter read (rAthena <c>pc_readparam</c>). Translator
    /// emits <c>h.getParam("Class")</c> etc. for the whitelisted
    /// param names.
    /// </summary>
    public int getParam(params object[] args)
    {
        if (args.Length == 0) return 0;
        var name = args[0]?.ToString() ?? "";
        return GetParamCore(name);
    }
    private int GetParamCore(string name) => name switch
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
