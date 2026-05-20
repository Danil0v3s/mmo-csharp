using Tools.RathenaImporter;
using Tools.RathenaImporter.Converters;

// Reads rAthena db/re/*.yml + db/*.yml and emits seed_*.sql files
// into Core.Database/Seeds/Scripts/. Run from the repo root:
//
//   dotnet run --project Tools.RathenaImporter -- \
//     --rathena /Volumes/1TB/Projetos/rathena \
//     --output  /Volumes/1TB/Projetos/mmo-csharp
//
// Or with no args — defaults to the sibling rathena/ checkout.

var rathenaRoot = "/Volumes/1TB/Projetos/rathena";
var outputRoot = "/Volumes/1TB/Projetos/mmo-csharp";

for (var i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--rathena") rathenaRoot = args[i + 1];
    if (args[i] == "--output") outputRoot = args[i + 1];
}

Console.WriteLine($"Tools.RathenaImporter");
Console.WriteLine($"  rathena root: {rathenaRoot}");
Console.WriteLine($"  output root:  {outputRoot}");
Console.WriteLine();

if (!Directory.Exists(rathenaRoot))
{
    Console.Error.WriteLine($"ERROR: rAthena directory not found: {rathenaRoot}");
    return 1;
}

// Register all converters here. Each one targets a single *_db table.
// Add new converters by implementing IYamlToSqlConverter and listing them.
var converters = new IYamlToSqlConverter[]
{
    new SkillDbConverter(),
    new AbraDbConverter(),
    new MagicMushroomDbConverter(),
    new SpellbookDbConverter(),
    new QuestDbConverter(),
    new PetDbConverter(),
    new AchievementDbConverter(),
    new HomunculusDbConverter(),
    new MercenaryDbConverter(),
    new InstanceDbConverter(),

    // Flat-shape converters.
    new CastleDbConverter(),
    new StatPointConverter(),
    new ExpHomunConverter(),
    new ExpGuildConverter(),
    new SizeFixConverter(),
    new ReputationConverter(),
    new CreateArrowDbConverter(),
    new ItemRandomOptDbConverter(),
    new CashShopDbConverter(),
    new CaptchaDbConverter(),

    // Payload (JSON column for nested structures) converters.
    new ElementalDbConverter(),
    new BattlegroundDbConverter(),
    new SkillTreeConverter(),
    new GuildSkillTreeConverter(),
    new MobSummonConverter(),
    new ItemRandomOptGroupConverter(),
    new AttrFixConverter(),
    new LevelPenaltyConverter(),
    new JobStatsConverter(),
    new JobExpConverter(),
    new JobBasePointsConverter(),
    new StatusYmlConverter(),
    new ItemCombosConverter(),
    new ItemPackagesConverter(),
    new ItemGroupDbConverter(),
    new ItemEnchantConverter(),
    new ItemReformConverter(),
    new LaphineSynthesisConverter(),
    new LaphineUpgradeConverter(),
    new RefineConverter(),
    new EnchantGradeConverter(),
    new MapDropsConverter(),
    new MobItemRatioConverter(),
    new ItemCashConverter(),
    new AttendanceConverter(),
    new ReputationGroupConverter(),
};

var ok = 0;
var failed = 0;

foreach (var c in converters)
{
    var src = Path.Combine(rathenaRoot, c.SourceYamlPath);
    if (!File.Exists(src))
    {
        Console.WriteLine($"  [skip] {c.Name}: source not found ({src})");
        continue;
    }

    try
    {
        Console.Write($"  [{c.Name}] reading {c.SourceYamlPath} ... ");
        var sql = await c.ConvertAsync(rathenaRoot);
        var outPath = Path.Combine(outputRoot, c.OutputSqlPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        await File.WriteAllTextAsync(outPath, sql);
        var lines = sql.Count(ch => ch == '\n');
        Console.WriteLine($"wrote {lines} lines → {c.OutputSqlPath}");
        ok++;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAILED: {ex.Message}");
        failed++;
    }
}

Console.WriteLine();
Console.WriteLine($"Done. {ok} converted, {failed} failed.");
return failed == 0 ? 0 : 2;
