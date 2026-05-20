using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Skills;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Skills;

/// <summary>
/// T2.4b+ closure — <see cref="SkillSideEffectService.StripEquip"/>
/// was a data-pending stub until T2.4a added the
/// <c>StatusType.Stripweapon</c> / <c>Stripshield</c> /
/// <c>Striparmor</c> / <c>Striphelm</c> enum slots. The strip mask
/// now resolves to one SC per equip slot.
/// </summary>
public class SkillSideEffectStripTests
{
    [Fact]
    public void StripWeapon_AttachesStripWeaponSc()
    {
        var ctx = Build();
        var target = ctx.AddPlayer(2);

        var ok = ctx.Service.StripEquip(ctx.Attacker, target, (int)EquipBits.HandR, durationMs: 10_000);

        Assert.True(ok);
        Assert.NotNull(ctx.Sc.Get(target, StatusType.Stripweapon));
    }

    [Fact]
    public void StripArmor_AttachesStripArmorSc()
    {
        var ctx = Build();
        var target = ctx.AddPlayer(2);

        var ok = ctx.Service.StripEquip(ctx.Attacker, target, (int)EquipBits.Armor, durationMs: 10_000);

        Assert.True(ok);
        Assert.NotNull(ctx.Sc.Get(target, StatusType.Striparmor));
        Assert.Null(ctx.Sc.Get(target, StatusType.Stripweapon));
    }

    [Fact]
    public void StripHelm_HandlesAllThreeHeadSlots()
    {
        var ctx = Build();
        var target = ctx.AddPlayer(2);

        ctx.Service.StripEquip(ctx.Attacker, target,
            (int)(EquipBits.HeadTop | EquipBits.HeadMid | EquipBits.HeadLow), durationMs: 10_000);

        Assert.NotNull(ctx.Sc.Get(target, StatusType.Striphelm));
    }

    [Fact]
    public void ZeroDuration_ReturnsFalse_AndAttachesNothing()
    {
        var ctx = Build();
        var target = ctx.AddPlayer(2);

        var ok = ctx.Service.StripEquip(ctx.Attacker, target, (int)EquipBits.Armor, durationMs: 0);

        Assert.False(ok);
        Assert.Null(ctx.Sc.Get(target, StatusType.Striparmor));
    }

    [Fact]
    public void Multibit_Mask_AttachesMultipleScs()
    {
        var ctx = Build();
        var target = ctx.AddPlayer(2);

        // Strip weapon + armor + helm in one mask.
        var mask = EquipBits.HandR | EquipBits.Armor | EquipBits.HeadTop;
        ctx.Service.StripEquip(ctx.Attacker, target, (int)mask, durationMs: 10_000);

        Assert.NotNull(ctx.Sc.Get(target, StatusType.Stripweapon));
        Assert.NotNull(ctx.Sc.Get(target, StatusType.Striparmor));
        Assert.NotNull(ctx.Sc.Get(target, StatusType.Striphelm));
    }

    // ---------- Test rig ----------

    private static TestContext Build()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var ids = new EntityIdAllocator();
        var damage = new DummyDamageService();
        var sc = new StatusChangeService(damage, entities, new StatusEffectRegistry(),
            NullLogger<StatusChangeService>.Instance);
        var svc = new SkillSideEffectService(new SkillDb(),
            NullLogger<SkillSideEffectService>.Instance, sc);

        var attacker = new PlayerEntity(1, 1, "A", Guid.NewGuid(),
            (uint)mapName.GetHashCode(), 100, 100);
        entities.Add(attacker);
        return new TestContext(svc, sc, entities, ids, (uint)mapName.GetHashCode(), attacker);
    }

    private sealed record TestContext(
        SkillSideEffectService Service,
        StatusChangeService Sc,
        EntityRegistry Entities,
        EntityIdAllocator Ids,
        uint MapId,
        PlayerEntity Attacker)
    {
        public PlayerEntity AddPlayer(int charId)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, 100, 100);
            Entities.Add(pc);
            return pc;
        }
    }

    private sealed class StubWorldRegistry : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorldRegistry(params MapData[] maps) =>
            _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }
}
