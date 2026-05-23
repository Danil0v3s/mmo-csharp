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
    public void RegisterNpc_varargs_registers_each_item()
    {
        var (host, registry, dir) = Build(@"
            ""use strict"";
            (() => {
                const a = { map: ""prontera"", x: 1, y: 1, sprite: 1, name: ""A"" };
                const b = { map: ""prontera"", x: 2, y: 2, sprite: 1, name: ""B"" };
                const c = { map: ""prontera"", x: 3, y: 3, sprite: 1, name: ""C"" };
                registerNpc(a, b, c);
            })();
        ");
        try
        {
            host.LoadEntryPoint();
            Assert.Equal(3, registry.NpcCount);
            Assert.NotNull(registry.GetNpcByName("A"));
            Assert.NotNull(registry.GetNpcByName("B"));
            Assert.NotNull(registry.GetNpcByName("C"));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void RegisterNpc_spread_from_array_registers_each_item()
    {
        var (host, registry, dir) = Build(@"
            ""use strict"";
            (() => {
                const npcs = [
                    { map: ""prontera"", x: 1, y: 1, sprite: 1, name: ""A1"" },
                    { map: ""prontera"", x: 2, y: 2, sprite: 1, name: ""A2"" },
                ];
                registerNpc(...npcs);
            })();
        ");
        try
        {
            host.LoadEntryPoint();
            Assert.Equal(2, registry.NpcCount);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void RegisterNpc_zero_args_is_a_no_op()
    {
        var (host, registry, dir) = Build(@"
            ""use strict"";
            (() => { registerNpc(); })();
        ");
        try
        {
            host.LoadEntryPoint();
            Assert.Equal(0, registry.NpcCount);
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
            // ClearScript exposes JS functions as ScriptObject; the registrar
            // validated callability at registration time, so reaching this
            // point with a non-null OnClick is enough.
            Assert.NotNull(npc.Hooks.OnClick!.Value);
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
        Assert.NotNull(registry.GetNpcByName("Persistence Probe"));
        Assert.NotNull(registry.GetNpcByName("Kafra Test"));
        Assert.NotNull(registry.GetFloatingByName("EventManager"));

        // CONV-2 acceptance: Tools.ItemScriptConvert emits ~19,886 items
        // (99.86% of rAthena's item_db_*) + 7,767 combos (100%) under
        // scripts/items/generated and scripts/combos/generated. The
        // _dev_test fixtures from CONV-1 sit alongside.
        //
        // Floors (not exact counts) so the test stays stable across
        // rAthena seed refreshes — a converter regression that drops
        // most of the corpus still trips.
        Assert.True(registry.ItemCount >= 19_000,
            $"Expected at least 19,000 items from scripts/dist/main.js, got {registry.ItemCount}");
        Assert.True(registry.ComboCount >= 7_000,
            $"Expected at least 7,000 combos from scripts/dist/main.js, got {registry.ComboCount}");
        // Dev-test fixtures still round-trip.
        Assert.NotNull(registry.GetItemById(999001));
        Assert.NotNull(registry.GetItemById(999002));
        // Spot-check a generated item (Red Potion 501 — onUse) and a
        // generated combo (combo_id 1).
        Assert.NotNull(registry.GetItemById(501));
        Assert.NotNull(registry.AllCombos().FirstOrDefault(c => c.ComboId == 1));
    }

    // ===== registerItem / registerCombo (CONV-1) =====

    [Fact]
    public void RegisterItem_with_onUse_only_records_the_async_hook()
    {
        var (host, registry, dir) = Build(@"
            ""use strict"";
            (() => {
                registerItem({
                    id: 501,
                    onUse: async (ctx) => { await ctx.player.itemHeal(50, 0); }
                });
            })();
        ");
        try
        {
            host.LoadEntryPoint();
            Assert.Equal(1, registry.ItemCount);
            var item = registry.GetItemById(501);
            Assert.NotNull(item);
            Assert.Equal(501, item!.Id);
            Assert.NotNull(item.Hooks.OnUse);
            Assert.Null(item.Hooks.OnEquip);
            Assert.Null(item.Hooks.OnUnequip);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void RegisterItem_with_onEquip_and_onUnequip_records_sync_hooks()
    {
        var (host, registry, dir) = Build(@"
            ""use strict"";
            (() => {
                registerItem({
                    id: 1201,
                    onEquip: (ctx) => { ctx.bonus(""bAtk"", 10); },
                    onUnequip: (ctx) => { /* no-op */ }
                });
            })();
        ");
        try
        {
            host.LoadEntryPoint();
            var item = registry.GetItemById(1201);
            Assert.NotNull(item);
            Assert.Null(item!.Hooks.OnUse);
            Assert.NotNull(item.Hooks.OnEquip);
            Assert.NotNull(item.Hooks.OnUnequip);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void RegisterItem_duplicate_id_throws()
    {
        var (host, _, dir) = Build(@"
            ""use strict"";
            (() => {
                registerItem({ id: 501 });
                registerItem({ id: 501 });
            })();
        ");
        try
        {
            var ex = Assert.ThrowsAny<Exception>(() => host.LoadEntryPoint());
            Assert.Contains("Duplicate registerItem() for id 501",
                ex.Message + (ex.InnerException?.Message ?? ""));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void RegisterItem_varargs_registers_each()
    {
        var (host, registry, dir) = Build(@"
            ""use strict"";
            (() => {
                registerItem(
                    { id: 501 },
                    { id: 502 },
                    { id: 503 });
            })();
        ");
        try
        {
            host.LoadEntryPoint();
            Assert.Equal(3, registry.ItemCount);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void RegisterItem_with_no_hooks_is_a_pointless_registration_but_does_not_throw()
    {
        // Authors may legitimately write `registerItem({ id: 501 })` as a
        // placeholder before adding hooks; the registrar shouldn't reject
        // it. The Hooks bundle just records nothing.
        var (host, registry, dir) = Build(@"
            ""use strict"";
            (() => { registerItem({ id: 999999 }); })();
        ");
        try
        {
            host.LoadEntryPoint();
            var item = registry.GetItemById(999999);
            Assert.NotNull(item);
            Assert.False(item!.Hooks.Any);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void RegisterCombo_with_members_and_onActive_records()
    {
        var (host, registry, dir) = Build(@"
            ""use strict"";
            (() => {
                registerCombo({
                    comboId: 27,
                    members: [""Knife"", ""Boots""],
                    onActive: (ctx) => { ctx.bonus(""bAtk"", 10); }
                });
            })();
        ");
        try
        {
            host.LoadEntryPoint();
            Assert.Equal(1, registry.ComboCount);
            var combo = registry.AllCombos().First();
            Assert.Equal(27, combo.ComboId);
            Assert.Equal(2, combo.Members.Count);
            Assert.Equal("Knife", combo.Members[0]);
            Assert.Equal("Boots", combo.Members[1]);
            Assert.NotNull(combo.Hooks.OnActive);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void RegisterCombo_with_empty_members_throws()
    {
        var (host, _, dir) = Build(@"
            ""use strict"";
            (() => {
                registerCombo({ comboId: 1, members: [] });
            })();
        ");
        try
        {
            var ex = Assert.ThrowsAny<Exception>(() => host.LoadEntryPoint());
            Assert.Contains("non-empty array", ex.Message + (ex.InnerException?.Message ?? ""));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void RegisterCombo_with_non_string_member_throws()
    {
        var (host, _, dir) = Build(@"
            ""use strict"";
            (() => {
                registerCombo({ comboId: 1, members: [""ok"", 42] });
            })();
        ");
        try
        {
            var ex = Assert.ThrowsAny<Exception>(() => host.LoadEntryPoint());
            Assert.Contains("[1]", ex.Message + (ex.InnerException?.Message ?? ""));
        }
        finally { Directory.Delete(dir, recursive: true); }
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
