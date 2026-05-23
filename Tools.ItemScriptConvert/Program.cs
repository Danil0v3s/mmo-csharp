using System.Text;
using Tools.ItemScriptConvert;

// =============================================================================
//   Tools.ItemScriptConvert
// -----------------------------------------------------------------------------
// Build-time converter: reads rAthena's item_db_* + item_combo_db SQL seeds,
// translates each script-column body via Map.Server's RathenaScriptParser +
// RathenaToJsTranslator (receiver name "ctx"), and emits TypeScript modules
// under scripts/items/generated/ + scripts/combos/generated/ that call
// registerItem({...}) / registerCombo({...}) — the same surface NPCs use.
//
// The generated tree is checked into git (debuggable, diff-able when rAthena
// updates land). Re-running the converter is idempotent within a seed
// revision.
//
// CLI:
//   dotnet run --project Tools.ItemScriptConvert -- [--root <repo-root>]
//
// Default --root is the parent of the running assembly's repo (auto-walks
// up looking for Core.Database/). Outputs are written under
// {root}/scripts/items/generated/ and {root}/scripts/combos/generated/.
//
// Bucket size is 500 ids — gives ~40 item files + ~16 combo files, big
// enough to keep file count reasonable, small enough to keep each file
// readable.
// =============================================================================

const int BucketSize = 500;
var root = ResolveRepoRoot(args);
Console.WriteLine($"[item-script-convert] repo root: {root}");

var itemSeeds = new[]
{
    Path.Combine(root, "Core.Database", "Seeds", "Scripts", "seed_item_db_equip.sql"),
    Path.Combine(root, "Core.Database", "Seeds", "Scripts", "seed_item_db_usable.sql"),
    Path.Combine(root, "Core.Database", "Seeds", "Scripts", "seed_item_db_etc.sql"),
};
var comboSeed = Path.Combine(root, "Core.Database", "Seeds", "Scripts", "seed_item_combos.sql");
var itemsOutDir = Path.Combine(root, "scripts", "items", "generated");
var combosOutDir = Path.Combine(root, "scripts", "combos", "generated");

// Group items by id-bucket. Each bucket aggregates every hook-kind for
// every id in the [lo, hi) range so a single registerItem() call gets
// all of an item's hooks together.
var itemHooks = new SortedDictionary<int, ItemHookSet>();
var itemStats = new Stats();
foreach (var seed in itemSeeds)
{
    Console.WriteLine($"[item-script-convert] reading {Path.GetFileName(seed)}");
    var sourceKind = SourceKindOf(seed);
    foreach (var row in SeedReader.ReadItems(seed))
    {
        itemStats.Seen++;
        var result = TsEmitter.TranslateBody(row.Body);
        if (!result.Ok)
        {
            itemStats.Skipped++;
            // Record on the hooks bag so we can emit a SKIPPED comment.
            if (!itemHooks.TryGetValue(row.Id, out var bag))
                itemHooks[row.Id] = bag = new ItemHookSet();
            bag.SkipReasons.Add($"{row.Kind}: {result.SkipReason}");
            continue;
        }
        itemStats.Ok++;
        if (!itemHooks.TryGetValue(row.Id, out var bag2))
            itemHooks[row.Id] = bag2 = new ItemHookSet();
        var hook = ChooseHookKind(row.Kind, sourceKind);
        // If two seed files declare the same id + same hook (e.g. an
        // override), last-write wins and we log the collision.
        if (bag2.Hooks.ContainsKey(hook))
            Console.WriteLine($"[item-script-convert]   override id={row.Id} hook={hook} (replacing earlier body)");
        bag2.Hooks[hook] = result.FunctionBody;
    }
}
Console.WriteLine($"[item-script-convert] item scripts: {itemStats.Ok} ok, {itemStats.Skipped} skipped, {itemStats.Seen} total");

// Combos — single file, one bucket dimension (combo_id).
var combos = new SortedDictionary<int, ComboEmit>();
var comboStats = new Stats();
Console.WriteLine($"[item-script-convert] reading {Path.GetFileName(comboSeed)}");
foreach (var row in SeedReader.ReadCombos(comboSeed))
{
    comboStats.Seen++;
    if (row.Members.Count == 0)
    {
        comboStats.Skipped++;
        continue; // registerCombo() rejects empty members
    }
    var result = TsEmitter.TranslateBody(row.Body);
    if (!result.Ok)
    {
        comboStats.Skipped++;
        combos[row.ComboId] = new ComboEmit(row.Members, null, result.SkipReason);
        continue;
    }
    comboStats.Ok++;
    combos[row.ComboId] = new ComboEmit(row.Members, result.FunctionBody, null);
}
Console.WriteLine($"[item-script-convert] combo scripts: {comboStats.Ok} ok, {comboStats.Skipped} skipped, {comboStats.Seen} total");

WriteItemBuckets(itemsOutDir, itemHooks, BucketSize);
WriteComboBuckets(combosOutDir, combos, BucketSize);

Console.WriteLine($"[item-script-convert] wrote items → {itemsOutDir}");
Console.WriteLine($"[item-script-convert] wrote combos → {combosOutDir}");

return 0;

// ---- helpers -----------------------------------------------------------------

static string ResolveRepoRoot(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == "--root") return Path.GetFullPath(args[i + 1]);
    }
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Core.Database")))
        dir = dir.Parent;
    if (dir == null)
        throw new DirectoryNotFoundException(
            "Couldn't locate repo root from assembly path. Pass --root explicitly.");
    return dir.FullName;
}

// File-name → coarse source classification. Drives the Script-column
// → onUse vs onEquip decision in ChooseHookKind.
static SourceKind SourceKindOf(string seedPath) => Path.GetFileName(seedPath) switch
{
    "seed_item_db_usable.sql" => SourceKind.Usable,
    "seed_item_db_equip.sql"  => SourceKind.Equip,
    "seed_item_db_etc.sql"    => SourceKind.Etc,
    _ => SourceKind.Unknown,
};

// rAthena's `script` column does double duty: for usable items it's an
// on-use action (potions, scrolls), for equip items it's the permanent
// on-equip bonus block. Cards (etc.) also use it as on-equip. We use the
// seed file as a coarse discriminator — accurate enough; finer-grained
// type-column inspection can land later if needed.
static HookKind ChooseHookKind(SeedReader.ScriptKind kind, SourceKind src) =>
    (kind, src) switch
    {
        (SeedReader.ScriptKind.EquipScript, _)            => HookKind.OnEquip,
        (SeedReader.ScriptKind.UnequipScript, _)          => HookKind.OnUnequip,
        (SeedReader.ScriptKind.Script, SourceKind.Usable) => HookKind.OnUse,
        (SeedReader.ScriptKind.Script, _)                 => HookKind.OnEquip,
        _ => HookKind.OnUse,
    };

static void WriteItemBuckets(
    string outDir, SortedDictionary<int, ItemHookSet> items, int bucketSize)
{
    if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
    Directory.CreateDirectory(outDir);

    var byBucket = items.GroupBy(kv => kv.Key / bucketSize * bucketSize)
                        .OrderBy(g => g.Key)
                        .ToList();
    foreach (var bucket in byBucket)
    {
        var lo = bucket.Key;
        var hi = lo + bucketSize - 1;
        var fileName = $"items_{lo:D6}_{hi:D6}.ts";
        File.WriteAllText(Path.Combine(outDir, fileName),
            BuildItemBucketFile(lo, hi, bucket.OrderBy(kv => kv.Key)));
    }
    File.WriteAllText(Path.Combine(outDir, "index.ts"),
        BuildIndexFile(byBucket.Select(b => $"items_{b.Key:D6}_{b.Key + bucketSize - 1:D6}")));
}

static void WriteComboBuckets(
    string outDir, SortedDictionary<int, ComboEmit> combos, int bucketSize)
{
    if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
    Directory.CreateDirectory(outDir);

    var byBucket = combos.GroupBy(kv => kv.Key / bucketSize * bucketSize)
                         .OrderBy(g => g.Key)
                         .ToList();
    foreach (var bucket in byBucket)
    {
        var lo = bucket.Key;
        var hi = lo + bucketSize - 1;
        var fileName = $"combos_{lo:D6}_{hi:D6}.ts";
        File.WriteAllText(Path.Combine(outDir, fileName),
            BuildComboBucketFile(lo, hi, bucket.OrderBy(kv => kv.Key)));
    }
    File.WriteAllText(Path.Combine(outDir, "index.ts"),
        BuildIndexFile(byBucket.Select(b => $"combos_{b.Key:D6}_{b.Key + bucketSize - 1:D6}")));
}

static string BuildItemBucketFile(int lo, int hi, IEnumerable<KeyValuePair<int, ItemHookSet>> items)
{
    var sb = new StringBuilder();
    sb.AppendLine("// AUTO-GENERATED by Tools.ItemScriptConvert. Do not hand-edit —");
    sb.AppendLine("// re-run `dotnet run --project Tools.ItemScriptConvert` instead.");
    sb.AppendLine($"// Item ids [{lo}..{hi}].");
    sb.AppendLine();
    foreach (var (id, bag) in items)
    {
        foreach (var skip in bag.SkipReasons)
            sb.Append("// SKIPPED id=").Append(id).Append(' ').AppendLine(skip);
        if (bag.Hooks.Count == 0) continue;
        sb.AppendLine("registerItem({");
        sb.Append("  id: ").Append(id).AppendLine(",");
        foreach (var (kind, body) in bag.Hooks)
        {
            var methodName = kind switch
            {
                HookKind.OnUse     => "onUse",
                HookKind.OnEquip   => "onEquip",
                HookKind.OnUnequip => "onUnequip",
                _ => throw new InvalidOperationException(),
            };
            // onUse is async (item-use packet awaits player ops);
            // onEquip / onUnequip are sync (game-loop equip recalc must
            // not suspend). The TS surface in api.d.ts enforces this.
            var async = kind == HookKind.OnUse ? "async " : "";
            sb.Append("  ").Append(async).Append(methodName).AppendLine("(ctx) {");
            sb.AppendLine(TsEmitter.Indent(body, spaces: 4));
            sb.AppendLine("  },");
        }
        sb.AppendLine("});");
        sb.AppendLine();
    }
    return sb.ToString();
}

static string BuildComboBucketFile(int lo, int hi, IEnumerable<KeyValuePair<int, ComboEmit>> combos)
{
    var sb = new StringBuilder();
    sb.AppendLine("// AUTO-GENERATED by Tools.ItemScriptConvert. Do not hand-edit —");
    sb.AppendLine("// re-run `dotnet run --project Tools.ItemScriptConvert` instead.");
    sb.AppendLine($"// Combo ids [{lo}..{hi}].");
    sb.AppendLine();
    foreach (var (comboId, emit) in combos)
    {
        if (emit.SkipReason != null)
            sb.Append("// SKIPPED comboId=").Append(comboId).Append(' ').AppendLine(emit.SkipReason);
        if (emit.Body == null) continue;
        sb.AppendLine("registerCombo({");
        sb.Append("  comboId: ").Append(comboId).AppendLine(",");
        sb.Append("  members: [");
        for (var i = 0; i < emit.Members.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(TsEmitter.TsString(emit.Members[i]));
        }
        sb.AppendLine("],");
        sb.AppendLine("  onActive(ctx) {");
        sb.AppendLine(TsEmitter.Indent(emit.Body, spaces: 4));
        sb.AppendLine("  },");
        sb.AppendLine("});");
        sb.AppendLine();
    }
    return sb.ToString();
}

static string BuildIndexFile(IEnumerable<string> bucketNames)
{
    var sb = new StringBuilder();
    sb.AppendLine("// AUTO-GENERATED by Tools.ItemScriptConvert. Do not hand-edit —");
    sb.AppendLine("// re-run `dotnet run --project Tools.ItemScriptConvert` instead.");
    sb.AppendLine("//");
    sb.AppendLine("// Side-effect imports each bucket file; every registerItem() /");
    sb.AppendLine("// registerCombo() call inside runs at module-evaluation time.");
    sb.AppendLine();
    foreach (var name in bucketNames)
        sb.Append("import \"./").Append(name).AppendLine("\";");
    return sb.ToString();
}

// ---- types -------------------------------------------------------------------

enum HookKind { OnUse, OnEquip, OnUnequip }
enum SourceKind { Unknown, Usable, Equip, Etc }

sealed class ItemHookSet
{
    public Dictionary<HookKind, string> Hooks { get; } = new();
    public List<string> SkipReasons { get; } = new();
}

sealed record ComboEmit(IReadOnlyList<string> Members, string? Body, string? SkipReason);

sealed class Stats
{
    public int Seen;
    public int Ok;
    public int Skipped;
}
