using System.Reflection;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Inventory.Script;
using Map.Server.Status;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Inventory.Script;

/// <summary>
/// DSL-4 acceptance smoke test: load every combo script from the
/// seeded SQL file and assert the V8 bridge evaluates each one
/// without throwing. The bridge is allowed to return false (parse /
/// translate / execute error) for individual scripts — those are
/// logged + counted; a single bad script can't take down the test,
/// the test fails only if the failure rate exceeds the known-baseline
/// threshold.
///
/// <para>
/// The seed file lives at <c>Core.Database/Seeds/Scripts/seed_item_combos.sql</c>
/// — 7,767 REPLACE statements with the rAthena combo script body in
/// the second value column. We extract scripts by regex (the SQL
/// shape is stable) rather than hitting MySQL, so the test stays
/// hermetic.
/// </para>
/// </summary>
public class ItemCombosSmokeTests
{
    [Fact]
    public void DiagnoseFirstV8Failure()
    {
        // Diagnostic: pick the first script that fails V8 exec, print
        // the translated JS + the V8 error so we can fix the host.
        var scripts = LoadCombosFromSeed();
        var svc = new ScriptedBonusService(NullLogger<ScriptedBonusService>.Instance);
        var pc = new PlayerEntity(1, 1, "X", Guid.NewGuid(), 0, 0, 0);
        var bundle = new EquipBonusBundle();
        var equipped = new[]
        {
            new InventoryItem { NameId = 1, Equip = EquipBonusAggregator.EquipRightHand, Refine = 12 },
        };

        foreach (var script in scripts)
        {
            bundle.Reset();
            try
            {
                var ast = RathenaScriptParser.Parse(script);
                var js = RathenaToJsTranslator.Translate(ast);
                using var engine = new Microsoft.ClearScript.V8.V8ScriptEngine();
                var host = new ScriptedBonusHost(pc, bundle, equipped);
                engine.AddHostObject("h", host);
                try { engine.Execute(js); }
                catch (Exception ex)
                {
                    // First V8 throw — dump everything and bail.
                    System.Console.WriteLine("=== first V8 failure ===");
                    System.Console.WriteLine("SCRIPT:");
                    System.Console.WriteLine(script);
                    System.Console.WriteLine("JS:");
                    System.Console.WriteLine(js);
                    System.Console.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
                    if (ex is Microsoft.ClearScript.ScriptEngineException sex)
                        System.Console.WriteLine($"DETAILS: {sex.ErrorDetails}");
                    return;
                }
            }
            catch { /* skip parse errors */ }
        }
    }

    [Fact]
    public void AllStockCombos_EvaluateThroughBridge_WithLowFailureRate()
    {
        var scripts = LoadCombosFromSeed();
        Assert.True(scripts.Count >= 7000,
            $"Expected ≥7000 combo scripts in the seed, got {scripts.Count} — seed file structure may have changed.");

        var svc = new ScriptedBonusService(NullLogger<ScriptedBonusService>.Instance, bonusSvc: null);
        var pc = new PlayerEntity(
            characterId: 1, accountId: 1, name: "SmokeTester",
            sessionId: Guid.NewGuid(), mapId: 0, x: 0, y: 0)
        {
            Level = 99, JobLevel = 50,
        };
        // Equip a high-refine weapon + shoes so conditional-on-refine
        // combos hit their then-branch and exercise more of the surface.
        var equipped = new[]
        {
            new InventoryItem { NameId = 1201, Equip = EquipBonusAggregator.EquipRightHand, Refine = 12 },
            new InventoryItem { NameId = 2401, Equip = EquipBonusAggregator.EquipShoes, Refine = 12 },
            new InventoryItem { NameId = 2301, Equip = EquipBonusAggregator.EquipArmor, Refine = 12 },
            new InventoryItem { NameId = 2501, Equip = EquipBonusAggregator.EquipGarment, Refine = 12 },
        };
        var bundle = new EquipBonusBundle();

        var failedByReason = new Dictionary<string, int>();
        var failedSamples = new Dictionary<string, string>();
        var ok = 0;
        foreach (var script in scripts)
        {
            bundle.Reset();
            try
            {
                // Parse first to capture parser errors distinctly.
                var ast = RathenaScriptParser.Parse(script);
                var js = RathenaToJsTranslator.Translate(ast);
                // Then try the full pipeline.
                if (svc.Apply(script, pc, bundle, equipped)) { ok++; continue; }
                // svc returned false → V8 exec error. Bucket as "v8-exec".
                Bump(failedByReason, failedSamples, "v8-exec", script);
            }
            catch (ScriptParseException ex)
            {
                Bump(failedByReason, failedSamples, $"parse:{Token(ex.Message)}", script);
            }
            catch (Exception ex)
            {
                Bump(failedByReason, failedSamples, $"other:{ex.GetType().Name}", script);
            }
        }

        var failedTotal = scripts.Count - ok;
        var failureRate = failedTotal * 100.0 / scripts.Count;

        // Emit a category breakdown so iterations on the parser/translator
        // can target the highest-impact buckets.
        var report = string.Join("\n", failedByReason
            .OrderByDescending(kv => kv.Value)
            .Take(10)
            .Select(kv => $"  {kv.Key}: {kv.Value} (sample: {Trunc(failedSamples[kv.Key], 100)})"));

        // Acceptance criterion (DSL-4 goal): every stock combo script
        // evaluates through the bridge without throwing. Achieved 0/7767
        // after the V8 host-Proxy fallback + parser extensions for
        // for-loops, postfix ++/--, indexed-LHS assignment, $-suffix
        // identifiers, @global-temp prefix, bitwise & |, ternary ?:,
        // and unary +. Anything > 0 fails the test so a regression in
        // the parser/translator/host surface surfaces immediately.
        Assert.True(failedTotal == 0,
            $"{failedTotal}/{scripts.Count} combos failed ({failureRate:F2}%).\n" +
            $"Top failure categories:\n{report}");
    }

    private static void Bump(Dictionary<string, int> counts, Dictionary<string, string> samples, string key, string sample)
    {
        if (!counts.TryGetValue(key, out var n)) counts[key] = 1;
        else counts[key] = n + 1;
        if (!samples.ContainsKey(key)) samples[key] = sample;
    }
    private static string Token(string msg)
    {
        var nl = msg.IndexOfAny(new[] { '\n', '\r' });
        return nl >= 0 ? msg[..nl] : msg;
    }
    private static string Trunc(string s, int n) =>
        s.Length <= n ? s.Replace('\n', ' ') : s[..n].Replace('\n', ' ') + "…";

    /// <summary>
    /// Parse the SQL seed file to extract the script-body column from
    /// each REPLACE INTO item_combo_db statement. The seed shape is:
    /// <c>REPLACE INTO `item_combo_db` (`combo_id`,`script`) VALUES (1,'bonus2 …');</c>.
    /// </summary>
    private static List<string> LoadCombosFromSeed()
    {
        var seedPath = ResolveSeedPath();
        if (!File.Exists(seedPath))
            throw new FileNotFoundException($"item_combos seed not found at {seedPath}");

        // Script bodies span multiple lines in the SQL (the converter
        // preserves rAthena's embedded \n as literal newlines inside
        // the quoted value). Read the whole file and match across
        // lines via the . metachar with Singleline mode.
        var rx = new System.Text.RegularExpressions.Regex(
            @"REPLACE INTO `item_combo_db` \(`combo_id`,`script`\) VALUES \(\d+,'((?:''|[^'])*)'\);",
            System.Text.RegularExpressions.RegexOptions.Compiled
            | System.Text.RegularExpressions.RegexOptions.Singleline);

        var content = File.ReadAllText(seedPath);
        var scripts = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in rx.Matches(content))
        {
            // Un-escape both SQL escapes: \\ → \ (must happen BEFORE
            // '' → ' to avoid double-replacing real escaped backslashes)
            // and '' → '.
            var body = m.Groups[1].Value.Replace("\\\\", "\\").Replace("''", "'");
            scripts.Add(body);
        }
        return scripts;
    }

    private static string ResolveSeedPath()
    {
        // Walk up from the test assembly until we find the repo root
        // (marked by Core.Database/), then descend to the seed file.
        var dir = new DirectoryInfo(Path.GetDirectoryName(
            Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Core.Database")))
            dir = dir.Parent;
        if (dir == null) throw new DirectoryNotFoundException("repo root not found from test assembly location");
        return Path.Combine(dir.FullName, "Core.Database", "Seeds", "Scripts", "seed_item_combos.sql");
    }
}
