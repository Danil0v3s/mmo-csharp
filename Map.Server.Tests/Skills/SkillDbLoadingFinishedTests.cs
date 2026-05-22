using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Skills;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Skills;

/// <summary>
/// SK.100-1a — verifies SkillDb.LoadingFinished + the new Combo
/// field + ISkillComboService consultation of skill_db combo chains.
/// </summary>
public class SkillDbLoadingFinishedTests
{
    [Fact]
    public void LoadingFinished_RunsAfterFallbackLoad_NoExceptions()
    {
        // The default SkillDb ctor calls LoadFallback then LoadingFinished;
        // construction itself is the test.
        var db = new SkillDb();
        Assert.True(db.Count > 0, "fallback catalog should populate");
        // Explicit re-run should also be safe.
        db.LoadingFinished();
    }

    [Fact]
    public void GetCombo_EmptyForLegacyCatalog()
    {
        var db = new SkillDb();
        // Pick a skill from the fallback catalog (SM_BASH = 5).
        var combo = db.GetCombo(skillId: 5);
        Assert.True(combo.IsEmpty, "fallback catalog has no Combo chains populated");
    }

    [Fact]
    public void SkillDefinition_Combo_DefaultsToEmpty()
    {
        var def = new SkillDefinition
        {
            Id = 999, Name = "TEST", MaxLevel = 1,
            Target = SkillTargetMode.TargetEnemy,
            DamageKind = SkillDamageKind.Weapon,
        };
        Assert.NotNull(def.Combo);
        Assert.Empty(def.Combo);
    }

    [Fact]
    public void Register_WithCombo_GetComboReturnsChain()
    {
        var db = new SkillDb();
        // Build a combo: skill 9999 → next-cast 8888.
        var withCombo = new SkillDefinition
        {
            Id = 9999, Name = "COMBO_HEAD", MaxLevel = 1,
            Target = SkillTargetMode.TargetEnemy,
            DamageKind = SkillDamageKind.Weapon,
            Combo = new ushort[] { 8888 },
        };
        var tail = new SkillDefinition
        {
            Id = 8888, Name = "COMBO_TAIL", MaxLevel = 1,
            Target = SkillTargetMode.TargetEnemy,
            DamageKind = SkillDamageKind.Weapon,
        };
        db.Register(tail);
        db.Register(withCombo, revalidate: true); // re-runs LoadingFinished

        var chain = db.GetCombo(9999);
        Assert.False(chain.IsEmpty);
        Assert.Equal((ushort)8888, chain[0]);
    }

    [Fact]
    public void LoadingFinished_UnresolvedComboRef_LogsWarning_DoesNotThrow()
    {
        // Register a combo head whose chain points to a non-existent
        // skill — LoadingFinished should warn but not throw.
        var db = new SkillDb();
        var broken = new SkillDefinition
        {
            Id = 9998, Name = "BROKEN_COMBO", MaxLevel = 1,
            Target = SkillTargetMode.TargetEnemy,
            DamageKind = SkillDamageKind.Weapon,
            Combo = new ushort[] { 7777 }, // 7777 doesn't exist
        };
        db.Register(broken, revalidate: true);
        // No throw == pass. Warning lands in NullLogger which we don't inspect.
    }

    [Fact]
    public void SkillComboService_IsCombo_ConsultsSkillDbChain()
    {
        // Build a SkillDb with a combo chain on a synthetic skill;
        // verify SkillComboService.IsCombo answers true for the
        // chained skill id.
        var db = new SkillDb();
        var head = new SkillDefinition
        {
            Id = 9997, Name = "TAEKWON_KICK_HEAD", MaxLevel = 1,
            Target = SkillTargetMode.TargetEnemy,
            DamageKind = SkillDamageKind.Weapon,
            Combo = new ushort[] { 9996 }, // chains into 9996
        };
        var tail = new SkillDefinition
        {
            Id = 9996, Name = "TAEKWON_KICK_TAIL", MaxLevel = 1,
            Target = SkillTargetMode.TargetEnemy,
            DamageKind = SkillDamageKind.Weapon,
        };
        db.Register(tail);
        db.Register(head, revalidate: true);

        var ctx = Build(skillDb: db);
        var caster = ctx.AddPlayer(1, 100, 100);
        // Caster just cast the head skill — it's the active combo.
        ctx.Combo.Combo(caster, skillId: 9997, skillLevel: 1, durationMs: 5000);

        Assert.True(ctx.Combo.IsCombo(caster, skillId: 9996),
            "chain-target skill should be in-combo per skill_db");
        Assert.False(ctx.Combo.IsCombo(caster, skillId: 9995),
            "unrelated skill should not be in-combo");
        Assert.True(ctx.Combo.IsCombo(caster, skillId: 9997),
            "legacy same-skill fallback still works");
    }

    private static TestContext Build(SkillDb skillDb)
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var combo = new SkillComboService(entities, NullLogger<SkillComboService>.Instance, skillDb);
        return new TestContext(combo, entities, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        SkillComboService Combo,
        EntityRegistry Entities,
        uint MapId)
    {
        public PlayerEntity AddPlayer(int charId, short x, short y)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", System.Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
        }
    }

    private sealed class StubWorldRegistry : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorldRegistry(params MapData[] maps) =>
            _maps = maps.ToDictionary(m => m.Name, System.StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }
}
