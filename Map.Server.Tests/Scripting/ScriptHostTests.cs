using Map.Server.Scripting;
using Map.Server.Scripting.Records;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Map.Server.Tests.Scripting;

/// <summary>
/// Smoke tests for the Jint-based scripting host. These tests construct a
/// fresh <see cref="ScriptHost"/> against a temp directory, write a small
/// JS file in the same shape esbuild emits (an IIFE that calls into
/// host-injected <c>register*()</c>), and assert the registry sees the
/// expected entries.
/// </summary>
public class ScriptHostTests
{
    private static (ScriptHost Host, NpcRegistry Registry, string Dir) Build(string bundleSource)
    {
        var dir = Path.Combine(Path.GetTempPath(), "mmo-script-host-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "main.js"), bundleSource);

        var registry = new NpcRegistry();
        var options = new ScriptHostOptions { ScriptsRoot = dir, EntryFile = "main.js" };
        var host = new ScriptHost(registry, options, NullLogger<ScriptHost>.Instance);
        return (host, registry, dir);
    }

    [Fact]
    public void Empty_bundle_loads_with_zero_registrations()
    {
        var (host, registry, dir) = Build("\"use strict\"; (() => {})();");
        try
        {
            host.LoadEntryPoint();
            Assert.Equal(0, registry.NpcCount);
            Assert.Equal(0, registry.FloatingCount);
            Assert.Equal(0, registry.ShopCount);
            Assert.Equal(0, registry.WarpCount);
            Assert.Equal(0, registry.SpawnCount);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void RegisterNpc_populates_registry()
    {
        var (host, registry, dir) = Build(@"
            ""use strict"";
            (() => {
                registerNpc({
                    map: ""prontera"", x: 160, y: 160, dir: 4,
                    sprite: 105, name: ""Test NPC"",
                    onClick: async (ctx) => { await ctx.mes(""hi""); }
                });
            })();
        ");
        try
        {
            host.LoadEntryPoint();
            Assert.Equal(1, registry.NpcCount);
            var npc = registry.GetNpcByName("Test NPC");
            Assert.NotNull(npc);
            Assert.Equal("prontera", npc!.Map);
            Assert.Equal((short)160, npc.X);
            Assert.Equal((short)160, npc.Y);
            Assert.Equal((byte)4, npc.Dir);
            Assert.Equal(105, npc.Sprite);
            Assert.NotNull(npc.Hooks.OnClick);
            Assert.True(npc.Hooks.OnClick!.IsCallable);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void RegisterFloatingNpc_rejects_world_position()
    {
        var (host, _, dir) = Build(@"
            ""use strict"";
            (() => {
                registerFloatingNpc({ name: ""bad"", map: ""prontera"", x: 10, y: 10 });
            })();
        ");
        try
        {
            var ex = Assert.ThrowsAny<Exception>(() => host.LoadEntryPoint());
            Assert.Contains("floating NPCs have no world position", ex.Message + (ex.InnerException?.Message ?? ""));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void RegisterShop_with_item_kind_requires_costItem()
    {
        var (host, _, dir) = Build(@"
            ""use strict"";
            (() => {
                registerShop({
                    kind: ""item"",
                    map: ""prontera"", x: 100, y: 100,
                    sprite: 73, name: ""Item Shop"",
                    items: [{ itemId: 501, price: 50 }]
                });
            })();
        ");
        try
        {
            var ex = Assert.ThrowsAny<Exception>(() => host.LoadEntryPoint());
            Assert.Contains("costItem", ex.Message + (ex.InnerException?.Message ?? ""));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void RegisterShop_market_kind_requires_stock()
    {
        var (host, registry, dir) = Build(@"
            ""use strict"";
            (() => {
                registerShop({
                    kind: ""market"",
                    map: ""prontera"", x: 100, y: 100,
                    sprite: 73, name: ""Market"",
                    items: [{ itemId: 501, price: 50, stock: 10 }]
                });
            })();
        ");
        try
        {
            host.LoadEntryPoint();
            Assert.Equal(1, registry.ShopCount);
            var shop = registry.AllShops().First();
            Assert.Equal(ShopKind.Market, shop.Kind);
            Assert.Equal(10, shop.Items[0].Stock);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void RegisterWarp_and_RegisterSpawn_populate_registry()
    {
        var (host, registry, dir) = Build(@"
            ""use strict"";
            (() => {
                registerWarp({
                    from: { map: ""prontera"", x: 156, y: 50 },
                    area: { xs: 1, ys: 1 },
                    to: { map: ""prt_fild05"", x: 158, y: 364 }
                });
                registerSpawn({
                    map: ""prt_fild05"", mobId: 1002, amount: 50,
                    respawn: { baseMs: 5000, jitterMs: 2000 }
                });
            })();
        ");
        try
        {
            host.LoadEntryPoint();
            Assert.Equal(1, registry.WarpCount);
            Assert.Equal(1, registry.SpawnCount);
            var warp = registry.AllWarps().First();
            Assert.Equal("prontera", warp.FromMap);
            Assert.Equal("prt_fild05", warp.ToMap);
            var spawn = registry.AllSpawns().First();
            Assert.Equal(1002, spawn.MobId);
            Assert.Equal(50, spawn.Amount);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void Duplicate_npc_name_throws()
    {
        var (host, _, dir) = Build(@"
            ""use strict"";
            (() => {
                registerNpc({ map: ""prontera"", x: 1, y: 1, sprite: 1, name: ""dup"" });
                registerNpc({ map: ""prontera"", x: 2, y: 2, sprite: 2, name: ""dup"" });
            })();
        ");
        try
        {
            var ex = Assert.ThrowsAny<Exception>(() => host.LoadEntryPoint());
            Assert.Contains("Duplicate NPC name", ex.Message + (ex.InnerException?.Message ?? ""));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void Non_callable_hook_throws()
    {
        var (host, _, dir) = Build(@"
            ""use strict"";
            (() => {
                registerNpc({
                    map: ""prontera"", x: 1, y: 1, sprite: 1, name: ""bad-hook"",
                    onClick: ""this is a string, not a function""
                });
            })();
        ");
        try
        {
            var ex = Assert.ThrowsAny<Exception>(() => host.LoadEntryPoint());
            Assert.Contains("onClick", ex.Message + (ex.InnerException?.Message ?? ""));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void Loads_real_scripts_dist_bundle()
    {
        // Walk up from the test binary to the repo root, then to scripts/dist/main.js.
        // bin/Debug/net9.0/ -> three "../" -> Map.Server.Tests/ -> one more "../" -> repo root.
        var bundlePath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "scripts", "dist", "main.js"));
        if (!File.Exists(bundlePath))
        {
            // The scripts bundle is built by `npm run build` in scripts/.
            // Skip the test cleanly when running in an environment without it.
            return;
        }

        var registry = new NpcRegistry();
        var options = new ScriptHostOptions
        {
            ScriptsRoot = Path.GetDirectoryName(bundlePath)!,
            EntryFile = "main.js",
        };
        var host = new ScriptHost(registry, options, NullLogger<ScriptHost>.Instance);
        host.LoadEntryPoint();

        // The dev-test fixture in scripts/npcs/_dev_test.ts registers two NPCs
        // and one floating NPC. Whatever else lands in the bundle later, those
        // three remain the Phase-1 acceptance baseline.
        Assert.True(registry.NpcCount >= 2,
            $"Expected at least 2 NPCs from scripts/dist/main.js, got {registry.NpcCount}");
        Assert.True(registry.FloatingCount >= 1,
            $"Expected at least 1 floating NPC from scripts/dist/main.js, got {registry.FloatingCount}");
        Assert.NotNull(registry.GetNpcByName("Phase 1 Test"));
        Assert.NotNull(registry.GetNpcByName("Kafra Test"));
        Assert.NotNull(registry.GetFloatingByName("EventManager"));
    }

    [Fact]
    public void Missing_bundle_logs_warning_and_creates_empty_engine()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mmo-missing-bundle-" + Guid.NewGuid().ToString("N"));
        var registry = new NpcRegistry();
        var options = new ScriptHostOptions { ScriptsRoot = dir, EntryFile = "main.js" };
        var host = new ScriptHost(registry, options, NullLogger<ScriptHost>.Instance);

        // Should not throw — missing bundle is a warning + empty engine
        host.LoadEntryPoint();
        Assert.Equal(0, registry.NpcCount);
    }
}
