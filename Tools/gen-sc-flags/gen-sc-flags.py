#!/usr/bin/env python3
"""
Generate Map.Server/Status/StatusCalcFlagDefaults.cs from rAthena's
db/re/status.yml CalcFlags table.

For each SC in status.yml, emits the list of BattleStats fields the SC
mutates. StatusEffectRegistry.RegisterDefaultsForMissingTypes() consumes
this to synthesize default OnStart/OnEnd handlers that apply Val1 deltas
to the listed stats.
"""
import re, sys, os

YML = "/Volumes/1TB/Projetos/rathena/db/re/status.yml"
TYPES = "/tmp/sc-names.txt"
OUT = "/Volumes/1TB/Projetos/mmo-csharp/Map.Server/Status/StatusCalcFlagDefaults.cs"

# rAthena CalcFlag name → C# CalcFlag enum value
# Names we route to a single BattleStats field. CalcFlag.None = skip.
CALC_FLAG_TO_STAT = {
    "Str": "Str", "Agi": "Agi", "Vit": "Vit",
    "Int": "IntStat",  # C# rename: Int → IntStat
    "Dex": "Dex", "Luk": "Luk",
    "Pow": "Pow", "Sta": "Sta", "Wis": "Wis", "Spl": "Spl", "Con": "Con", "Crt": "Crt",
    "MaxHp": "MaxHp", "MaxSp": "MaxSp",
    "Hit": "Hit", "Flee": "Flee", "Flee2": "Flee2", "Cri": "Cri",
    "Def": "Def", "Def2": "Def2", "Mdef": "Mdef", "MDef2": "Mdef2",
    "Aspd": "AspdRate",  # rAthena Aspd CalcFlag → C# AspdRate field
    "Batk": "Batk",
    "Patk": "Patk", "Smatk": "Smatk", "Res": "Res", "Mres": "Mres",
    "Hplus": "Hplus", "Crate": "Crate",
    # Watk/Matk: rAthena recalcs both min/max. Use Batk as the "weapon
    # ATK marker" stat — closer to rAthena semantics than 0.
    "Watk": "Batk",
    "Matk": "Batk",  # MAtk has no separate field; collapse to Batk
    # Speed: no direct stat; use AspdRate as proxy (matches the
    # NS-3 wave 1 Quagmire / WindWalk / CartBoost pattern).
    "Speed": "AspdRate",
    # All: expands to 6 base stats (rAthena status_change.cpp).
    # The generator handles this specially.
    "All": "All",
    # Skip — no direct stat field, presence-only:
    "Regen": None,        # tick-rate marker for IPcRegenService
    "Atk_Ele": None,      # element override (combat-side)
    "Def_Ele": None,      # element override (combat-side)
    "Mode": None,         # mob AI mode flag
    "Dspd": None,         # display speed
    "Dye": None,          # cosmetic
}

# CalcFlag.All expansion (rAthena status_change.cpp: All ⇒ STR/AGI/VIT/INT/DEX/LUK).
ALL_STATS = ["Str", "Agi", "Vit", "IntStat", "Dex", "Luk"]


def normalize_yml_name(name):
    """rAthena status names use any casing; the C# StatusType enum uses
    PascalCase with specific overrides (DeadlyPoison, IncreaseAgi, …).
    Match by case-insensitive lookup against the enum names list."""
    # The yml uses CamelCase with underscores in places — strip
    # underscores + lowercase for comparison.
    return name.replace("_", "").lower()


def parse_status_yml():
    """Returns list of (yml_name, [calc_flags])."""
    entries = []
    current = None
    in_calc = False
    with open(YML, "r") as f:
        for raw in f:
            line = raw.rstrip()
            m = re.match(r"^  - Status:\s*(\S+)", line)
            if m:
                if current:
                    entries.append(current)
                current = (m.group(1), [])
                in_calc = False
                continue
            if not current:
                continue
            if re.match(r"^    CalcFlags:\s*$", line):
                in_calc = True
                continue
            if in_calc:
                # CalcFlag entry: "      Foo: true"
                m2 = re.match(r"^      ([A-Za-z][A-Za-z0-9_]*):\s*(true|false)\s*$", line)
                if m2:
                    flag, val = m2.group(1), m2.group(2)
                    if val == "true":
                        current[1].append(flag)
                    continue
                # Anything indented less than 6 ends the CalcFlags block.
                if line and not line.startswith("      "):
                    in_calc = False
        if current:
            entries.append(current)
    return entries


def build_enum_map():
    """Map lowercase-underscore-stripped names to actual StatusType enum identifier."""
    m = {}
    with open(TYPES, "r") as f:
        for line in f:
            name = line.strip()
            if not name or name == "None":
                continue
            key = normalize_yml_name(name)
            m[key] = name
    return m


def main():
    entries = parse_status_yml()
    enum_map = build_enum_map()

    # Build SC → field list, resolving CalcFlag.All to 6 base stats and
    # dropping the SC entirely if all flags map to None.
    sc_to_fields = {}
    skipped_unknown = []
    no_calcflag = []
    for yml_name, flags in entries:
        enum_name = enum_map.get(normalize_yml_name(yml_name))
        if not enum_name:
            skipped_unknown.append(yml_name)
            continue
        fields = []
        for f in flags:
            mapped = CALC_FLAG_TO_STAT.get(f)
            if mapped is None:
                continue
            if mapped == "All":
                fields.extend(ALL_STATS)
            else:
                fields.append(mapped)
        # De-dup, preserve order.
        seen = set()
        uniq = [x for x in fields if not (x in seen or seen.add(x))]
        if uniq:
            sc_to_fields[enum_name] = uniq
        else:
            no_calcflag.append(enum_name)

    print(f"# parsed {len(entries)} status.yml SCs", file=sys.stderr)
    print(f"# {len(sc_to_fields)} SCs with at least one mapped CalcFlag", file=sys.stderr)
    print(f"# {len(no_calcflag)} SCs with only-presence flags (no stat mod)", file=sys.stderr)
    print(f"# {len(skipped_unknown)} status.yml names without StatusType enum entry", file=sys.stderr)

    # Emit C# source.
    lines = [
        "// <auto-generated/>",
        "// Generated 2026-05-24 by /tmp/gen-sc-flags.py from",
        "// /Volumes/1TB/Projetos/rathena/db/re/status.yml.",
        "//",
        "// Re-run the generator when status.yml updates upstream. The",
        "// table here is the single source of truth for which",
        "// BattleStats fields each SC modifies; consumed by",
        "// StatusEffectRegistry.RegisterDefaultsForMissingTypes() to",
        "// synthesize default OnStart/OnEnd handlers that apply Val1",
        "// deltas to the listed stats — closing the SC handler depth",
        "// gap from 48 hand-ported → 1,000+ generated.",
        "//",
        "// Explicit Register(StatusType.X, new StatusEffectHandler(...))",
        "// calls in StatusEffectRegistry override the generated handlers",
        "// (dictionary overwrite wins), so per-SC bespoke formulas",
        "// (Berserk's +200 flat Batk, Blessing's val1 to 3 stats, etc.)",
        "// take precedence where they exist.",
        "using System.Collections.Generic;",
        "",
        "namespace Map.Server.Status;",
        "",
        "/// <summary>",
        "/// Map of <see cref=\"StatusType\"/> → list of <see cref=\"CalcStatField\"/>",
        "/// the SC modifies. Populated from rAthena <c>db/re/status.yml</c>",
        "/// CalcFlags by the generator script above. Used by",
        "/// <see cref=\"StatusEffectRegistry\"/> to default-register every SC",
        "/// with a real stat-mod body before the explicit Register() calls",
        "/// in the registry's ctor override the high-priority ones.",
        "/// </summary>",
        "public static class StatusCalcFlagDefaults",
        "{",
        "    /// <summary>BattleStats field names this SC's Val1 mod scales.</summary>",
        "    public static IReadOnlyList<CalcStatField> For(StatusType type)",
        "        => _table.GetValueOrDefault(type, System.Array.Empty<CalcStatField>());",
        "",
        "    /// <summary>How many SCs have at least one mapped CalcFlag.</summary>",
        "    public static int Count => _table.Count;",
        "",
        "    private static readonly Dictionary<StatusType, CalcStatField[]> _table = new()",
        "    {",
    ]

    for sc_name in sorted(sc_to_fields.keys()):
        fields = sc_to_fields[sc_name]
        fields_csv = ", ".join(f"CalcStatField.{f}" for f in fields)
        lines.append(f"        [StatusType.{sc_name}] = new[] {{ {fields_csv} }},")

    lines.extend([
        "    };",
        "}",
        "",
        "/// <summary>",
        "/// Enum of BattleStats fields a generated SC handler can mutate.",
        "/// One value per non-presence CalcFlag in rAthena status.yml.",
        "/// Used by <see cref=\"StatusCalcFlagDefaults\"/> + the registry's",
        "/// default-handler synthesizer.",
        "/// </summary>",
        "public enum CalcStatField",
        "{",
    ])
    # Emit the enum: all the unique target field names we use.
    used_fields = set()
    for fields in sc_to_fields.values():
        used_fields.update(fields)
    for f in sorted(used_fields):
        lines.append(f"    {f},")
    lines.extend([
        "}",
        "",
    ])

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w") as f:
        f.write("\n".join(lines))
    print(f"# wrote {OUT}: {len(sc_to_fields)} SC entries, {len(used_fields)} CalcStatField enum values", file=sys.stderr)


if __name__ == "__main__":
    main()
