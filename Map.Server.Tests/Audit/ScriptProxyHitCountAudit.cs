using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Inventory.Script;
using Map.Server.Scripting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Audit;

/// <summary>
/// NS-1b · Script-Proxy hit-count audit. Instruments the
/// <c>__invokeHookWithCtx</c> Proxy so every fallback lookup
/// (`ctx.&lt;method&gt;` not found on the host) is tallied, drives
/// every <c>onEquip</c> + <c>onActive</c> hook through it, and writes
/// the frequency table to <c>/tmp/proxy-hits.txt</c>.
///
/// <para>
/// The output is the per-rAthena-builtin priority list for NS-2 (wiring
/// <see cref="ScriptedBonusHost"/> <c>/* data-pending */</c> methods).
/// Top entries = highest-impact host methods to fill in first.
/// </para>
///
/// <para>
/// Filtered out of the default test run with the <c>audit</c> trait
/// so it doesn't bloat regular sweeps. Run via:
/// <code>dotnet test Map.Server.Tests --filter "audit=proxy-hits"</code>
/// </para>
/// </summary>
public class ScriptProxyHitCountAudit
{
    private static string? FindBundle()
    {
        var path = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "scripts", "dist", "main.js"));
        return File.Exists(path) ? path : null;
    }

    private static (ScriptHost host, NpcRegistry registry) LoadBundle()
    {
        var bundle = FindBundle()
            ?? throw new InvalidOperationException(
                "scripts/dist/main.js missing — run `npm run build` in scripts/.");
        var registry = new NpcRegistry();
        var options = new ScriptHostOptions
        {
            ScriptsRoot = Path.GetDirectoryName(bundle)!,
            EntryFile = "main.js",
        };
        var host = new ScriptHost(registry, options, NullLogger<ScriptHost>.Instance);
        host.LoadEntryPoint();
        return (host, registry);
    }

    [Fact]
    [Trait("audit", "proxy-hits")]
    public void HarvestProxyHitCounts()
    {
        var (host, registry) = LoadBundle();
        try
        {
            // Replace the default Proxy invoker with an instrumented one
            // that tallies every fallback (=== name resolved through the
            // no-op return) into globalThis.__proxyHits. The shape mirrors
            // ScriptHost.InstallHookInvoker exactly, plus the counting +
            // a separate map for "real method calls" so we can compute
            // what % of calls fall through to the no-op.
            host.Engine.Execute(@"
                globalThis.__proxyHits = new Map();
                globalThis.__realHits  = new Map();
                globalThis.__invokeHookWithCtx = function(fn, rawCtx) {
                    const ctx = new Proxy(rawCtx, {
                        get: function(t, p) {
                            // Skip non-string keys (Symbol.toPrimitive etc.)
                            if (typeof p !== 'string') return t[p];
                            const v = t[p];
                            if (v === undefined || v === null) {
                                const c = (__proxyHits.get(p) ?? 0) + 1;
                                __proxyHits.set(p, c);
                                return function() { return 0; };
                            }
                            // Only count fn-shaped reads (the script is
                            // calling a method, not reading a sub-object
                            // like ctx.player). Sub-object reads still
                            // need to be visible — those don't dispatch
                            // through the fallback so they're not the
                            // priority gap.
                            if (typeof v === 'function') {
                                const c = (__realHits.get(p) ?? 0) + 1;
                                __realHits.set(p, c);
                            }
                            return v;
                        }
                    });
                    return fn(ctx);
                };
            ");

            // Drive every combo's onActive + every item's onEquip through
            // the instrumented invoker. Re-uses the maxed PC + equipped
            // set pattern from ComboDispatcherTests so getrefine() /
            // getequiprefinerycnt() etc. have plausible values.
            var pc = new PlayerEntity(
                characterId: 1, accountId: 1, name: "ProxyAudit",
                sessionId: Guid.NewGuid(), mapId: 0, x: 0, y: 0)
            {
                Level = 99, JobLevel = 50,
            };
            var equipped = new[]
            {
                new InventoryItem { NameId = 1201, Equip = EquipBonusAggregator.EquipRightHand, Refine = 12 },
                new InventoryItem { NameId = 2401, Equip = EquipBonusAggregator.EquipShoes,     Refine = 12 },
                new InventoryItem { NameId = 2301, Equip = EquipBonusAggregator.EquipArmor,     Refine = 12 },
                new InventoryItem { NameId = 2501, Equip = EquipBonusAggregator.EquipGarment,   Refine = 12 },
            };
            var bundleBag = new EquipBonusBundle();
            var invoker = host.Engine.Script.__invokeHookWithCtx;

            var fired = 0;
            foreach (var combo in registry.AllCombos())
            {
                if (combo.Hooks.OnActive is not { } handle) continue;
                bundleBag.Reset();
                var ctx = new ScriptedBonusHost(pc, bundleBag, equipped);
                try { invoker(handle.Value, ctx); fired++; } catch { /* counted via __proxyHits */ }
            }
            foreach (var item in registry.AllItems())
            {
                if (item.Hooks.OnEquip is not { } handle) continue;
                bundleBag.Reset();
                var ctx = new ScriptedBonusHost(pc, bundleBag, equipped);
                try { invoker(handle.Value, ctx); fired++; } catch { /* counted via __proxyHits */ }
            }

            // Pull the Map back as an array of [name, count] pairs via JS
            // Array.from — ClearScript marshals that into something we can
            // enumerate from C#.
            dynamic proxyEntries = host.Engine.Script.Array.from(host.Engine.Script.__proxyHits);
            dynamic realEntries  = host.Engine.Script.Array.from(host.Engine.Script.__realHits);

            var proxyHits = new List<(string Name, int Count)>();
            for (int i = 0; i < (int)proxyEntries.length; i++)
            {
                dynamic pair = proxyEntries[i];
                proxyHits.Add(((string)pair[0], (int)pair[1]));
            }
            var realHits = new List<(string Name, int Count)>();
            for (int i = 0; i < (int)realEntries.length; i++)
            {
                dynamic pair = realEntries[i];
                realHits.Add(((string)pair[0], (int)pair[1]));
            }

            // Sort + emit. Highest-frequency Proxy fallback first.
            proxyHits.Sort((a, b) => b.Count.CompareTo(a.Count));
            realHits.Sort((a, b) => b.Count.CompareTo(a.Count));

            var outPath = Path.Combine(Path.GetTempPath(), "proxy-hits.txt");
            using var w = new StreamWriter(outPath);
            w.WriteLine($"# Script-Proxy hit-count audit (NS-1b)");
            w.WriteLine($"# Hooks fired: {fired}");
            w.WriteLine($"# Unknown methods routed through the no-op Proxy fallback: {proxyHits.Count} distinct names, {proxyHits.Sum(x => x.Count)} total hits");
            w.WriteLine($"# Known (host-defined) methods called: {realHits.Count} distinct names, {realHits.Sum(x => x.Count)} total hits");
            w.WriteLine();
            w.WriteLine("## Proxy fallback (= rAthena builtins not surfaced on ScriptedBonusHost — these silently return 0)");
            w.WriteLine();
            w.WriteLine("| Count | Method |");
            w.WriteLine("|---:|---|");
            foreach (var (name, count) in proxyHits)
                w.WriteLine($"| {count} | `{name}` |");
            w.WriteLine();
            w.WriteLine("## Real host calls (= methods that ScriptedBonusHost actually answers)");
            w.WriteLine();
            w.WriteLine("| Count | Method |");
            w.WriteLine("|---:|---|");
            foreach (var (name, count) in realHits)
                w.WriteLine($"| {count} | `{name}` |");

            // Echo top-20 to stdout so the test log captures the headline.
            Console.WriteLine($"=== Proxy fallback top-20 (of {proxyHits.Count}) ===");
            foreach (var (n, c) in proxyHits.Take(20))
                Console.WriteLine($"  {c,8}  {n}");
            Console.WriteLine($"=== Real-host top-20 (of {realHits.Count}) ===");
            foreach (var (n, c) in realHits.Take(20))
                Console.WriteLine($"  {c,8}  {n}");
            Console.WriteLine($"=== Full table written to {outPath} ===");

            Assert.True(fired > 1000, $"Only fired {fired} hooks — bundle may not have loaded");
        }
        finally
        {
            host.Dispose();
        }
    }
}
