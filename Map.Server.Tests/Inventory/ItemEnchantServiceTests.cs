using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Inventory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Inventory;

public class ItemEnchantServiceTests
{
    [Fact]
    public void Reload_BuildsPipelineTreeAndIndexesByTarget()
    {
        var svc = NewService(
            roots: new[] { Root(101, minRefine: 7, resetChance: 50000, resetPrice: 100000) },
            targets: new[] { Target(101, "Dragonic_Slayer") },
            materials: new[]
            {
                Mat(101, slot: -1, "Elunium", 5), // reset cost
                Mat(101, slot: 0, "Bloody_Edge", 1),
            },
            slots: new[]
            {
                Slot(101, slot: 0, price: 500000, orderIndex: 0),
                Slot(101, slot: 1, price: 750000, orderIndex: 1),
            },
            options: new[]
            {
                Option(101, slot: 0, grade: 0, "Slayer_Lv1_Bonus"),
                Option(101, slot: 0, grade: 0, "Slayer_Atk_Plus"),
                Option(101, slot: 0, grade: 1, "Slayer_Lv2_Bonus"),
                Option(101, slot: 1, grade: 0, "Crit_Plus"),
            });

        Assert.True(svc.IsLoaded);
        Assert.Equal(1, svc.PipelineCount);

        var p = svc.GetPipelineForItem("Dragonic_Slayer");
        Assert.NotNull(p);
        Assert.Equal(101, p!.EnchantId);
        Assert.Equal(7, p.MinimumRefine);

        // Case-insensitive target lookup
        Assert.NotNull(svc.GetPipelineForItem("DRAGONIC_SLAYER"));
        Assert.Null(svc.GetPipelineForItem("Unknown"));
        Assert.Null(svc.GetPipelineForItem(""));
    }

    [Fact]
    public void GetEnchantOptions_FiltersBySlotAndGrade()
    {
        var svc = NewService(
            roots: new[] { Root(101) },
            targets: new[] { Target(101, "X") },
            materials: Array.Empty<ItemEnchantMaterialDbEntity>(),
            slots: new[] { Slot(101, 0, 100, 0) },
            options: new[]
            {
                Option(101, 0, 0, "A"),
                Option(101, 0, 0, "B"),
                Option(101, 0, 1, "C"),
                Option(101, 1, 0, "D"),
            });

        var s0g0 = svc.GetEnchantOptions(101, 0, 0);
        Assert.Equal(2, s0g0.Count);
        Assert.Contains(s0g0, o => o.OptionItemAegis == "A");
        Assert.Contains(s0g0, o => o.OptionItemAegis == "B");

        var s0g1 = svc.GetEnchantOptions(101, 0, 1);
        Assert.Single(s0g1);
        Assert.Equal("C", s0g1[0].OptionItemAegis);

        Assert.Empty(svc.GetEnchantOptions(101, 99, 0)); // unknown slot
        Assert.Empty(svc.GetEnchantOptions(99, 0, 0)); // unknown pipeline
    }

    [Fact]
    public void GetResetCost_ReturnsChancePriceAndResetMaterials()
    {
        var svc = NewService(
            roots: new[] { Root(202, resetChance: 30000, resetPrice: 50000) },
            targets: new[] { Target(202, "Item") },
            materials: new[]
            {
                Mat(202, slot: -1, "Elunium", 3),
                Mat(202, slot: -1, "Oridecon", 2),
                Mat(202, slot: 0, "Other", 1), // per-slot, NOT for reset
            },
            slots: Array.Empty<ItemEnchantSlotDbEntity>(),
            options: Array.Empty<ItemEnchantOptionDbEntity>());

        var cost = svc.GetResetCost(202);
        Assert.NotNull(cost);
        Assert.Equal(30000, cost!.Value.chance);
        Assert.Equal(50000, cost.Value.price);
        Assert.Equal(2, cost.Value.materials.Count);
        Assert.All(cost.Value.materials, m => Assert.Equal(-1, m.Slot));

        Assert.Null(svc.GetResetCost(999));
    }

    [Fact]
    public void GetSlotCost_ReturnsPriceMaterialsAndOrderIndex()
    {
        var svc = NewService(
            roots: new[] { Root(303) },
            targets: new[] { Target(303, "Item") },
            materials: new[]
            {
                Mat(303, slot: 0, "Mat0", 1),
                Mat(303, slot: 0, "Mat0b", 2),
                Mat(303, slot: 1, "Mat1", 1),
            },
            slots: new[]
            {
                Slot(303, slot: 0, price: 100, orderIndex: 2),
                Slot(303, slot: 1, price: 200, orderIndex: null),
            },
            options: Array.Empty<ItemEnchantOptionDbEntity>());

        var s0 = svc.GetSlotCost(303, 0);
        Assert.NotNull(s0);
        Assert.Equal(100, s0!.Value.price);
        Assert.Equal(2, s0.Value.orderIndex);
        Assert.Equal(2, s0.Value.materials.Count);

        var s1 = svc.GetSlotCost(303, 1);
        Assert.NotNull(s1);
        Assert.Null(s1!.Value.orderIndex);
        Assert.Single(s1.Value.materials);

        Assert.Null(svc.GetSlotCost(303, 99));
        Assert.Null(svc.GetSlotCost(999, 0));
    }

    [Fact]
    public void EmptyCatalog_IsLoadedFalse()
    {
        var svc = new ItemEnchantService(NullLogger<ItemEnchantService>.Instance);
        Assert.False(svc.IsLoaded);
        Assert.Equal(0, svc.PipelineCount);
        Assert.Null(svc.GetPipelineForItem("Anything"));
        Assert.Null(svc.GetPipeline(1));
        Assert.Null(svc.GetResetCost(1));
        Assert.Null(svc.GetSlotCost(1, 0));
        Assert.Empty(svc.GetEnchantOptions(1, 0, 0));
    }

    // ---- helpers ----

    private static ItemEnchantDbEntity Root(int id, int minRefine = 0, int resetChance = 0, int resetPrice = 0) => new()
    {
        EnchantId = id,
        MinimumRefine = minRefine,
        ResetChance = resetChance,
        ResetPrice = resetPrice,
    };
    private static ItemEnchantTargetDbEntity Target(int id, string aegis) => new() { EnchantId = id, ItemAegis = aegis };
    private static ItemEnchantMaterialDbEntity Mat(int id, int slot, string aegis, int amt) => new()
    {
        EnchantId = id, Slot = slot, MaterialAegis = aegis, Amount = amt,
    };
    private static ItemEnchantSlotDbEntity Slot(int id, int slot, int price, int? orderIndex) => new()
    {
        EnchantId = id, Slot = slot, Price = price, OrderIndex = orderIndex,
    };
    private static ItemEnchantOptionDbEntity Option(int id, int slot, int grade, string aegis) => new()
    {
        EnchantId = id, Slot = slot, EnchantGrade = grade, OptionItemAegis = aegis,
    };

    private static ItemEnchantService NewService(
        ItemEnchantDbEntity[] roots,
        ItemEnchantTargetDbEntity[] targets,
        ItemEnchantMaterialDbEntity[] materials,
        ItemEnchantSlotDbEntity[] slots,
        ItemEnchantOptionDbEntity[] options)
    {
        var services = new ServiceCollection();
        services.AddScoped<IItemEnchantDbRepository>(_ => new StubRepo(
            roots.ToList(), targets.ToList(), materials.ToList(), slots.ToList(), options.ToList()));
        var provider = services.BuildServiceProvider();
        return new ItemEnchantService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ItemEnchantService>.Instance);
    }

    private sealed class StubRepo : IItemEnchantDbRepository
    {
        private readonly List<ItemEnchantDbEntity> _roots;
        private readonly List<ItemEnchantTargetDbEntity> _targets;
        private readonly List<ItemEnchantMaterialDbEntity> _mats;
        private readonly List<ItemEnchantSlotDbEntity> _slots;
        private readonly List<ItemEnchantOptionDbEntity> _options;

        public StubRepo(
            List<ItemEnchantDbEntity> roots,
            List<ItemEnchantTargetDbEntity> targets,
            List<ItemEnchantMaterialDbEntity> mats,
            List<ItemEnchantSlotDbEntity> slots,
            List<ItemEnchantOptionDbEntity> options)
        {
            _roots = roots; _targets = targets; _mats = mats; _slots = slots; _options = options;
        }

        public Task<IReadOnlyList<ItemEnchantDbEntity>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ItemEnchantDbEntity>>(_roots);
        public Task<ItemEnchantDbEntity?> GetByIdAsync(int enchantId, CancellationToken ct = default)
            => Task.FromResult(_roots.FirstOrDefault(r => r.EnchantId == enchantId));
        public Task<IReadOnlyList<ItemEnchantTargetDbEntity>> GetAllTargetsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ItemEnchantTargetDbEntity>>(_targets);
        public Task<IReadOnlyList<ItemEnchantTargetDbEntity>> GetTargetsAsync(int enchantId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ItemEnchantTargetDbEntity>>(_targets.Where(t => t.EnchantId == enchantId).ToList());
        public Task<IReadOnlyList<ItemEnchantMaterialDbEntity>> GetMaterialsAsync(int enchantId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ItemEnchantMaterialDbEntity>>(_mats.Where(m => m.EnchantId == enchantId).ToList());
        public Task<IReadOnlyList<ItemEnchantSlotDbEntity>> GetSlotsAsync(int enchantId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ItemEnchantSlotDbEntity>>(_slots.Where(s => s.EnchantId == enchantId).ToList());
        public Task<IReadOnlyList<ItemEnchantOptionDbEntity>> GetOptionsAsync(int enchantId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ItemEnchantOptionDbEntity>>(_options.Where(o => o.EnchantId == enchantId).ToList());
    }
}
