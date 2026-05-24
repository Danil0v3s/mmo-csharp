using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Status;
using Map.Server.Visibility;

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
    private readonly IEntityRegistry? _entities;
    private readonly Skills.IPlayerSkillService? _skillSvc;
    private readonly IPlayerOptionService? _optionSvc;
    private readonly IVisibilityService? _visibility;

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
        IPlayerBonusService? bonusSvc = null,
        IEntityRegistry? entities = null,
        Skills.IPlayerSkillService? skillSvc = null,
        IPlayerOptionService? optionSvc = null,
        IVisibilityService? visibility = null)
    {
        _pc = pc;
        _bundle = bundle;
        _equipped = equipped;
        _catalog = catalog;
        _bonusSvc = bonusSvc;
        _entities = entities;
        _skillSvc = skillSvc;
        _optionSvc = optionSvc;
        _visibility = visibility;
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

    /// <summary>
    /// rAthena <c>skill "SkillName", level, [type];</c> — grants a skill
    /// while the item is equipped. NS-2b wire (726 calls in NS-1b
    /// harvest, most-called of the silently-no-op host methods).
    /// Resolves the aegis name (e.g. <c>"AB_RENOVATIO"</c>) to a numeric
    /// skill id via reflection over <see cref="Skills.SkillIds"/>,
    /// then calls <see cref="Skills.IPlayerSkillService.Grant"/>. Defaults
    /// to <see cref="Skills.GrantKind.Temporary"/> — matches rAthena's
    /// ADDSKILL_TEMP semantics for equip-granted skills (revoked when
    /// the item un-equips).
    /// </summary>
    public void skill(params object[] args)
    {
        if (args.Length < 2 || _skillSvc == null) return;
        var skillId = ResolveSkillId(args[0]);
        if (skillId == 0) return;
        var level = ToInt(args[1]);
        var kind = args.Length > 2 ? (Skills.GrantKind)ToInt(args[2]) : Skills.GrantKind.Temporary;
        _skillSvc.Grant(_pc, skillId, level, kind);
    }

    /// <summary>
    /// rAthena <c>heal hp, sp;</c> — clamps +hp into [0, MaxHp] and +sp
    /// into [0, MaxSp]. Negative values damage. NS-2b wire.
    /// </summary>
    public void heal(params object[] args)
    {
        if (args.Length == 0) return;
        var hp = ToInt(args[0]);
        var sp = args.Length > 1 ? ToInt(args[1]) : 0;
        if (hp != 0) _pc.Hp = Math.Clamp(_pc.Hp + hp, 0, _pc.MaxHp);
        if (sp != 0) _pc.Sp = Math.Clamp(_pc.Sp + sp, 0, _pc.MaxSp);
    }

    /// <summary>
    /// rAthena <c>percentheal hpPct, spPct;</c> — restores
    /// MaxHp * hpPct / 100 and MaxSp * spPct / 100, clamped. NS-2b wire.
    /// </summary>
    public void percentheal(params object[] args)
    {
        if (args.Length == 0) return;
        var hpPct = ToInt(args[0]);
        var spPct = args.Length > 1 ? ToInt(args[1]) : 0;
        if (hpPct != 0)
        {
            var delta = _pc.MaxHp * hpPct / 100;
            _pc.Hp = Math.Clamp(_pc.Hp + delta, 0, _pc.MaxHp);
        }
        if (spPct != 0)
        {
            var delta = _pc.MaxSp * spPct / 100;
            _pc.Sp = Math.Clamp(_pc.Sp + delta, 0, _pc.MaxSp);
        }
    }

    /// <summary>
    /// rAthena <c>itemheal hp, sp;</c> — heal scaled by item heal rates.
    /// First slice: identical to <see cref="heal"/>. The rAthena variant
    /// applies <c>battle_config.item_heal_rate</c> and per-PC <c>HPrecovRate</c>
    /// / <c>SPrecovRate</c> bonuses, which would need to be wired through
    /// <c>PlayerBonusService</c>. Data-pending against that bonus path;
    /// behavior parity for the common 100% case is exact.
    /// </summary>
    public void itemheal(params object[] args) => heal(args);

    /// <summary>
    /// rAthena <c>specialeffect effectId;</c> — broadcasts a cosmetic
    /// effect to everyone in AOI (the affected entity included).
    /// NS-3 wave 6: wired through <see cref="IVisibilityService.SendToArea"/>
    /// emitting <see cref="ZC_NOTIFY_EFFECT2"/> (rAthena
    /// <c>clif_specialeffect</c>). Effect ids come from rAthena
    /// <c>effect_list.txt</c> — the client is the consumer.
    /// </summary>
    public void specialeffect(params object[] args)
    {
        if (args.Length == 0 || _visibility == null) return;
        var effectId = ToInt(args[0]);
        _visibility.SendToArea(_pc,
            new ZC_NOTIFY_EFFECT2 { EntityId = _pc.Id.Value, EffectId = effectId },
            SendTarget.Area);
    }

    /// <summary>
    /// rAthena <c>specialeffect2 effectId;</c> — sends the effect to
    /// the script invoker only (self target). Most autobonus item scripts
    /// use this form (e.g. <c>specialeffect2 EF_POTION_BERSERK;</c> on
    /// a weapon proc).
    /// </summary>
    public void specialeffect2(params object[] args)
    {
        if (args.Length == 0 || _visibility == null) return;
        var effectId = ToInt(args[0]);
        _visibility.SendToSelf(_pc,
            new ZC_NOTIFY_EFFECT2 { EntityId = _pc.Id.Value, EffectId = effectId });
    }

    /// <summary>
    /// rAthena <c>hateffect effectId, state;</c> — toggles a cosmetic
    /// hat-effect overlay. Requires <c>ZC_HAT_EFFECT</c> (0x0a3b) which
    /// is variable-length + carries an effect-id array; we model it as
    /// a single-state toggle here by emitting <see cref="ZC_NOTIFY_EFFECT2"/>
    /// as a fallback. The hat-effect-specific packet adds purely visual
    /// state preservation across map changes — game-relevant behavior
    /// (does the hat appear?) lands at the emitter even with the
    /// simpler packet. Full ZC_HAT_EFFECT port deferred until the
    /// costume sprite pipeline ports.
    /// </summary>
    public void hateffect(params object[] args)
    {
        if (args.Length == 0 || _visibility == null) return;
        var effectId = ToInt(args[0]);
        // state arg (args[1]) is ignored — our fallback emits the
        // effect once; rAthena's ZC_HAT_EFFECT toggle semantics aren't
        // preserved (the effect plays once instead of staying).
        _visibility.SendToArea(_pc,
            new ZC_NOTIFY_EFFECT2 { EntityId = _pc.Id.Value, EffectId = effectId },
            SendTarget.Area);
    }

    /// <summary>
    /// rAthena <c>petloot count;</c> — sets the pet's auto-loot
    /// capacity (max items the pet can collect from kills before it
    /// has to deposit). The cap is stored on the player's active pet
    /// entity. If the PC has no pet (e.g. the script ran before pet
    /// summon), it's a no-op — rAthena silently does the same.
    ///
    /// <para>The actual auto-loot pickup behavior is handled by the
    /// pet AI step; this method just sets the per-pet cap.</para>
    /// </summary>
    public void petloot(params object[] args)
    {
        if (args.Length == 0 || _entities == null) return;
        var count = ToInt(args[0]);
        if (count < 0) count = 0;
        // Find the player's active pet by master id == pc.Id.
        foreach (var entity in _entities.All())
        {
            if (entity is PetEntity pet && pet.MasterId == _pc.Id.Value)
            {
                pet.AutoLootMax = count;
                break;
            }
        }
    }

    /// <summary>
    /// rAthena <c>setoption flag, [enable];</c> — toggle bits on
    /// <see cref="PlayerEntity.Option"/>. With 1 arg, sets the option
    /// to that value; with 2 args and enable!=0, adds bits, enable==0
    /// removes. NS-2b wire — was silently no-op pre-NS-2b.
    /// </summary>
    public void setoption(params object[] args)
    {
        if (args.Length == 0 || _optionSvc == null) return;
        var flag = (PlayerOption)(uint)ToInt(args[0]);
        if (args.Length == 1)
        {
            _optionSvc.SetOption(_pc, flag);
            return;
        }
        var enable = ToInt(args[1]) != 0;
        if (enable) _optionSvc.AddOption(_pc, flag);
        else        _optionSvc.RemoveOption(_pc, flag);
    }

    /// <summary>
    /// rAthena <c>message "..."</c> — sends a system message line to
    /// the script invoker (one of the two channels under
    /// <c>clif_displaymessage</c>). NS-3 wave 6: wired via
    /// <see cref="ZC_NOTIFY_PLAYERCHAT"/> (0x008e).
    /// </summary>
    public void message(params object[] args)
    {
        if (args.Length == 0 || _visibility == null) return;
        var text = args[0]?.ToString() ?? string.Empty;
        _visibility.SendToSelf(_pc, new ZC_NOTIFY_PLAYERCHAT { Message = text });
    }

    /// <summary>
    /// rAthena <c>dispbottom "..."</c> — bottom-of-screen system
    /// message (subset of <c>clif_displaymessage</c>; same packet
    /// as <see cref="message"/> in our port). The rAthena variant
    /// can carry a color hex code; we drop it for now since the
    /// 0x008e payload doesn't include a color slot — the client
    /// applies the chat-area default.
    /// </summary>
    public void dispbottom(params object[] args)
    {
        if (args.Length == 0 || _visibility == null) return;
        var text = args[0]?.ToString() ?? string.Empty;
        _visibility.SendToSelf(_pc, new ZC_NOTIFY_PLAYERCHAT { Message = text });
    }

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
    /// <summary>
    /// rAthena <c>getequipid(EQI_SLOT)</c> — id of the item equipped in
    /// the given slot, or 0 if nothing is equipped there.
    /// </summary>
    public int getequipid(params object[] args)
    {
        if (args.Length == 0 || _equipped == null) return 0;
        var item = FindEquippedInSlot(args[0]?.ToString() ?? "");
        return item == null ? 0 : (int)item.NameId;
    }

    /// <summary>
    /// rAthena <c>getequipweaponlv([EQI_SLOT])</c> — weapon level of the
    /// equipped item in the given slot (default <c>EQI_HAND_R</c>).
    /// Reads from <see cref="Core.Database.Entities.ItemEntity.WeaponLevel"/>
    /// via <see cref="_catalog"/>. NS-2a wire — previously hit the Proxy
    /// fallback (13 calls in NS-1b harvest).
    /// </summary>
    public int getequipweaponlv(params object[] args)
    {
        if (_equipped == null || _catalog == null) return 0;
        var slot = args.Length == 0 ? "EQI_HAND_R" : args[0]?.ToString() ?? "EQI_HAND_R";
        var item = FindEquippedInSlot(slot);
        if (item == null) return 0;
        var row = _catalog.Get(item.NameId);
        return row?.WeaponLevel ?? 0;
    }

    /// <summary>
    /// rAthena <c>getequiparmorlv([EQI_SLOT])</c> — armor level of the
    /// equipped item in the given slot (default <c>EQI_ARMOR</c>).
    /// NS-2a wire — previously hit the Proxy fallback (9 calls in NS-1b).
    /// </summary>
    public int getequiparmorlv(params object[] args)
    {
        if (_equipped == null || _catalog == null) return 0;
        var slot = args.Length == 0 ? "EQI_ARMOR" : args[0]?.ToString() ?? "EQI_ARMOR";
        var item = FindEquippedInSlot(slot);
        if (item == null) return 0;
        var row = _catalog.Get(item.NameId);
        return row?.ArmorLevel ?? 0;
    }

    /// <summary>
    /// rAthena <c>getenchantgrade([EQI_SLOT])</c> — enchant grade (0..4
    /// = grade D..A in rAthena enchantgrade system) of the equipped
    /// item in the given slot. Reads from
    /// <see cref="InventoryItem.EnchantGrade"/>. NS-2a wire — this was
    /// the #1 Proxy fallback hit (1,239 calls, 89% of all fallbacks in
    /// NS-1b harvest). Combos like Illusion 100% sets gate per-grade
    /// bonuses on this value.
    /// </summary>
    public int getenchantgrade(params object[] args)
    {
        if (_equipped == null) return 0;
        var slot = args.Length == 0 ? "EQI_HAND_R" : args[0]?.ToString() ?? "EQI_HAND_R";
        var item = FindEquippedInSlot(slot);
        return item?.EnchantGrade ?? 0;
    }

    /// <summary>
    /// rAthena <c>getitempos(itemId)</c> — equip-mask bits the item is
    /// currently bound to, or 0 if not equipped. NS-2a wire (11 calls
    /// in NS-1b). Used by multi-slot items to branch on which slot
    /// they're currently filling.
    /// </summary>
    public int getitempos(params object[] args)
    {
        if (args.Length == 0 || _equipped == null) return 0;
        var itemId = (uint)ToInt(args[0]);
        for (var i = 0; i < _equipped.Count; i++)
        {
            var it = _equipped[i];
            if (it.NameId == itemId && it.Equip != 0) return (int)it.Equip;
        }
        return 0;
    }

    /// <summary>
    /// rAthena <c>vip_status(type, [charName])</c> — VIP info query.
    /// Types (rAthena <c>VIP_STATUS_*</c>):
    ///   1 = is VIP (1/0),
    ///   2 = expiration unix timestamp,
    ///   3 = remaining seconds.
    /// NS-3 wave 6: reads <see cref="PlayerEntity.VipExpireTimestamp"/>
    /// hydrated from the account login record. The charName arg is
    /// ignored (we don't have a cross-character VIP lookup yet —
    /// would require an IPC to the login server; rAthena 95% of the
    /// time queries self anyway).
    /// </summary>
    public int vip_status(params object[] args)
    {
        var type = args.Length > 0 ? ToInt(args[0]) : 1;
        var expiry = (int)_pc.VipExpireTimestamp;
        var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return type switch
        {
            1 => expiry > now ? 1 : 0,
            2 => expiry,
            3 => expiry > now ? expiry - now : 0,
            _ => 0,
        };
    }

    /// <summary>
    /// rAthena <c>gettime(type)</c> — see script.cpp BUILDIN_FUNC(gettime).
    /// Types (script_constants.hpp <c>DT_*</c>):
    /// 1=second, 2=minute, 3=hour, 4=day-of-week (Sun=0..Sat=6),
    /// 5=day-of-month, 6=month, 7=year, 8=day-of-year.
    /// NS-2a wire (3 calls in NS-1b). UTC-based; matches rAthena's
    /// reliance on the server-local clock for daily-reset gates like
    /// item 12133 (McDonald's Ice Cone).
    /// </summary>
    public int gettime(params object[] args)
    {
        if (args.Length == 0) return 0;
        var type = ToInt(args[0]);
        var now = DateTime.UtcNow;
        return type switch
        {
            1 => now.Second,
            2 => now.Minute,
            3 => now.Hour,
            4 => (int)now.DayOfWeek,
            5 => now.Day,
            6 => now.Month,
            7 => now.Year,
            8 => now.DayOfYear,
            _ => 0,
        };
    }

    /// <summary>
    /// Resolve an EQI_* slot token to the equipped item filling it.
    /// Shared by <see cref="getequipid"/>, <see cref="getequipweaponlv"/>,
    /// <see cref="getequiparmorlv"/>, <see cref="getenchantgrade"/>.
    /// Returns null if the slot is empty or the token isn't recognized.
    /// </summary>
    private InventoryItem? FindEquippedInSlot(string slotToken)
    {
        if (_equipped == null) return null;
        var bits = EquipBitsForSlot(slotToken);
        if (bits == 0) return null;
        for (var i = 0; i < _equipped.Count; i++)
        {
            var item = _equipped[i];
            if ((item.Equip & bits) != 0) return item;
        }
        return null;
    }

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

    /// <summary>
    /// rAthena <c>getpetinfo(type)</c> — see script.cpp:15911 BUILDIN_FUNC(getpetinfo).
    /// Returns 0 (or empty string for PETINFO_NAME) when the owner has no
    /// live pet. <paramref name="args"/>[0] may be the integer enum value
    /// (0..9) or the constant name ("PETINFO_EGGID" etc.) — both forms
    /// arrive from the TS/V8 bridge depending on whether the script was
    /// hand-ported or converter-emitted.
    ///
    /// <para>
    /// Used by item scripts like Wonder Egg Basket (id 15980 / 410027 /
    /// 410028) and Beast Rings (490405) to branch on the egg the pet
    /// hatched from. PETINFO_EGGID resolves against
    /// <see cref="PetEntity.EggId"/>, populated by
    /// <see cref="Pet.IPetService.Summon"/> at hatch time.
    /// </para>
    /// </summary>
    public object getpetinfo(params object[] args)
    {
        if (args.Length == 0) return 0;
        var typeArg = args[0];
        var type = ResolvePetInfoType(typeArg);
        var pet = FindLivePet();
        if (pet == null)
        {
            // rAthena returns "null" string for PETINFO_NAME, 0 otherwise.
            return type == 2 ? (object)"null" : 0;
        }
        return type switch
        {
            0 => (int)pet.PetId,            // PETINFO_ID
            1 => pet.ClassId,               // PETINFO_CLASS (mob_db class)
            2 => pet.PetName ?? "",         // PETINFO_NAME
            3 => (int)pet.Intimacy,         // PETINFO_INTIMATE
            4 => (int)pet.Hunger,           // PETINFO_HUNGRY
            5 => 0,                         // PETINFO_RENAMED — no live flag yet
            6 => pet.Level,                 // PETINFO_LEVEL
            7 => pet.Id.Value,              // PETINFO_BLOCKID
            8 => pet.EggId,                 // PETINFO_EGGID
            9 => 0,                         // PETINFO_FOODID — needs pet_db food row
            _ => 0,
        };
    }

    private static int ResolvePetInfoType(object? arg) => arg switch
    {
        int i => i,
        long l => (int)l,
        double d => (int)d,
        float f => (int)f,
        string s => s switch
        {
            "PETINFO_ID" or "PET_ID" => 0,
            "PETINFO_CLASS" or "PET_CLASS" => 1,
            "PETINFO_NAME" or "PET_NAME" => 2,
            "PETINFO_INTIMATE" or "PET_INTIMATE" => 3,
            "PETINFO_HUNGRY" or "PET_HUNGRY" => 4,
            "PETINFO_RENAMED" => 5,
            "PETINFO_LEVEL" => 6,
            "PETINFO_BLOCKID" => 7,
            "PETINFO_EGGID" => 8,
            "PETINFO_FOODID" => 9,
            _ => int.TryParse(s, out var n) ? n : -1,
        },
        _ => -1,
    };

    private Entities.PetEntity? FindLivePet()
    {
        if (_entities == null) return null;
        foreach (var e in _entities.All())
            if (e is Entities.PetEntity pet && pet.MasterId == _pc.Id) return pet;
        return null;
    }

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
        var item = FindEquippedInSlot(slotToken);
        return item?.Refine ?? 0;
    }

    /// <summary>
    /// Cached aegis-name → skill-id lookup, built once via reflection
    /// over <see cref="Skills.SkillIds"/>. The constants there (e.g.
    /// <c>SM_BASH = 5</c>) are the runtime source of truth for the
    /// aegis-name → numeric-id mapping; reflecting them here lets
    /// <see cref="skill"/> accept TS calls like
    /// <c>ctx.skill("AB_RENOVATIO", 4)</c> without a hand-maintained
    /// dictionary. Lookup is O(1) after first build; the cache lives
    /// for the process lifetime.
    /// </summary>
    private static readonly Dictionary<string, ushort> _skillIdByName =
        BuildSkillIdLookup();

    private static Dictionary<string, ushort> BuildSkillIdLookup()
    {
        var map = new Dictionary<string, ushort>(StringComparer.Ordinal);
        var fields = typeof(Skills.SkillIds).GetFields(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Static);
        foreach (var f in fields)
        {
            if (!f.IsLiteral || f.FieldType != typeof(ushort)) continue;
            var value = (ushort)(f.GetRawConstantValue() ?? (ushort)0);
            if (value != 0) map[f.Name] = value;
        }
        return map;
    }

    /// <summary>
    /// Resolve a skill identifier from a script call argument. Accepts
    /// the aegis name string (looked up against <see cref="_skillIdByName"/>)
    /// or a numeric id passthrough. Returns 0 for unknown names — the
    /// caller treats 0 as "no-op", matching rAthena's
    /// <c>script_get_skillid</c> behavior.
    /// </summary>
    private static ushort ResolveSkillId(object? arg)
    {
        if (arg == null) return 0;
        if (arg is int i) return (ushort)i;
        if (arg is double d) return (ushort)(int)d;
        var name = arg.ToString();
        if (string.IsNullOrEmpty(name)) return 0;
        return _skillIdByName.GetValueOrDefault(name);
    }

    /// <summary>
    /// Translate an rAthena EQI_* slot constant string to its
    /// <see cref="EquipBonusAggregator"/> bit. Costume / shadow / head-mid
    /// / head-low fall through to 0 until the aggregator surface adds
    /// them. Returns 0 for unknown tokens.
    /// </summary>
    private static uint EquipBitsForSlot(string slotToken) => slotToken switch
    {
        "EQI_HEAD_TOP"      => EquipBonusAggregator.EquipHelm,
        "EQI_HAND_R"        => EquipBonusAggregator.EquipRightHand,
        "EQI_HAND_L"        => EquipBonusAggregator.EquipLeftHand,
        "EQI_ARMOR"         => EquipBonusAggregator.EquipArmor,
        "EQI_GARMENT"       => EquipBonusAggregator.EquipGarment,
        "EQI_SHOES"         => EquipBonusAggregator.EquipShoes,
        "EQI_ACC_R"         => EquipBonusAggregator.EquipAccessoryR,
        "EQI_ACC_L"         => EquipBonusAggregator.EquipAccessoryL,
        _                   => 0u,
    };

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
