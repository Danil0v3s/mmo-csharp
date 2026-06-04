using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Mob;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using DbMob = Core.Database.Entities.MobEntity;

namespace Map.Server.Tests.MobDatabase;

/// <summary>
/// AI-MVP — the mob_skill_db loader. Until this landed, <see cref="MobDbEntry.Skills"/> was always
/// empty, so no mob (MVP or trash) ever cast a skill. These pin that the seeded rows are loaded onto
/// the right mob, in priority (row) order, with the rAthena state/condition/target strings mapped
/// onto the runtime enums the <see cref="MobSkillCastService"/> picker consumes.
/// </summary>
public class MobSkillLoadTests
{
    private const int NineTails = 1613; // an MVP-ish id; arbitrary for the stub

    [Fact]
    public void Skills_are_loaded_onto_the_matching_mob_in_row_order()
    {
        var db = NewMobDb(
            mobs: new[] { Mob(NineTails, "NineTail"), Mob(1002, "Poring") },
            skills: new[]
            {
                Row(NineTails, "NPC_SUMMONSLAVE", state: "attack", skillId: 196, rate: 10000, condition: "slavele", condValue: "2", target: "self"),
                Row(NineTails, "AL_HEAL", state: "attack", skillId: 28, rate: 5000, condition: "myhpltmaxrate", condValue: "50", target: "self", chat: "1500"),
                Row(1002, "NPC_EMOTION", state: "loot", skillId: 197, rate: 2000, condition: "always", target: "self"),
            });

        var mvp = db.Get(NineTails)!;
        Assert.Equal(2, mvp.Skills.Count);
        // row order preserved (priority)
        Assert.Equal(196, mvp.Skills[0].SkillId);
        Assert.Equal(28, mvp.Skills[1].SkillId);
        Assert.Single(db.Get(1002)!.Skills);
    }

    [Fact]
    public void Row_maps_state_condition_target_and_payload()
    {
        var db = NewMobDb(
            mobs: new[] { Mob(NineTails, "NineTail") },
            skills: new[]
            {
                Row(NineTails, "AL_HEAL", state: "attack", skillId: 28, lv: 5, rate: 5000, delay: 8000,
                    cancelable: "yes", condition: "myhpltmaxrate", condValue: "50", target: "self", emotion: "1", chat: "1500"),
            });

        var s = db.Get(NineTails)!.Skills.Single();
        Assert.Equal(28, s.SkillId);
        Assert.Equal(5, s.SkillLevel);
        Assert.Equal(MobSkillState.Berserk, s.State);        // "attack" → Berserk
        Assert.Equal(MobSkillCondition.MyHpLessThanRate, s.Condition);
        Assert.Equal(50, s.Cond2);                            // HP% threshold
        Assert.Equal(MobSkillTarget.Self, s.Target);
        Assert.Equal(5000, s.Permillage);
        Assert.Equal(8000, s.DelayMs);
        Assert.True(s.Cancelable);
        Assert.Equal(1, s.Emotion);
        Assert.Equal(1500, s.ChatId);                         // HP-threshold announce chat
    }

    [Theory]
    [InlineData("attack", MobSkillState.Berserk)]
    [InlineData("chase", MobSkillState.Rush)]
    [InlineData("angry", MobSkillState.Angry)]
    [InlineData("idle", MobSkillState.Idle)]
    [InlineData("follow", MobSkillState.Follow)]
    [InlineData("loot", MobSkillState.Loot)]
    [InlineData("dead", MobSkillState.Dead)]
    [InlineData("any", MobSkillState.Any)]
    public void State_strings_map_to_enum(string raw, MobSkillState expected)
    {
        var db = NewMobDb(new[] { Mob(NineTails, "x") },
            new[] { Row(NineTails, "s", state: raw, skillId: 10, target: "self") });
        Assert.Equal(expected, db.Get(NineTails)!.Skills.Single().State);
    }

    [Theory]
    [InlineData("target", MobSkillTarget.Target)]
    [InlineData("randomtarget", MobSkillTarget.Random)]
    [InlineData("self", MobSkillTarget.Self)]
    [InlineData("friend", MobSkillTarget.Friend)]
    [InlineData("master", MobSkillTarget.Master)]
    [InlineData("around2", MobSkillTarget.Around2)]
    public void Target_strings_map_to_enum(string raw, MobSkillTarget expected)
    {
        var db = NewMobDb(new[] { Mob(NineTails, "x") },
            new[] { Row(NineTails, "s", state: "attack", skillId: 10, target: raw) });
        Assert.Equal(expected, db.Get(NineTails)!.Skills.Single().Target);
    }

    // --- harness ---

    private static DbMob Mob(int id, string aegis) => new()
    {
        Id = (uint)id, NameAegis = aegis, NameEnglish = aegis, Level = 50, Hp = 1000, Sp = 1,
    };

    private static MobSkillDbEntity Row(int mobId, string skillName, string state, short skillId,
        byte lv = 1, short rate = 5000, int delay = 5000, string cancelable = "no",
        string condition = "always", string? condValue = null, string target = "target",
        string? emotion = null, string? chat = null) => new()
    {
        MobId = (short)mobId, Info = $"Mob@{skillName}", State = state, SkillId = skillId, SkillLv = lv,
        Rate = rate, CastTime = 0, Delay = delay, Cancelable = cancelable, Target = target,
        Condition = condition, ConditionValue = condValue, Emotion = emotion, Chat = chat,
    };

    private static Map.Server.Mob.MobDb NewMobDb(IEnumerable<DbMob> mobs, IEnumerable<MobSkillDbEntity> skills)
    {
        var services = new ServiceCollection();
        services.AddScoped<IMobRepository>(_ => new StubMobRepository(mobs.ToArray()));
        services.AddScoped<IMobSkillDbRepository>(_ => new StubMobSkillRepository(skills.ToArray()));
        var provider = services.BuildServiceProvider();
        return new Map.Server.Mob.MobDb(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<Map.Server.Mob.MobDb>.Instance);
    }

    private sealed class StubMobRepository : IMobRepository
    {
        private readonly List<DbMob> _rows;
        public StubMobRepository(params DbMob[] rows) => _rows = rows.ToList();
        public Task<List<DbMob>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(_rows.ToList());
        public Task<DbMob?> GetByIdAsync(uint mobId, CancellationToken ct = default) => Task.FromResult(_rows.FirstOrDefault(r => r.Id == mobId));
        public Task<DbMob?> GetByAegisNameAsync(string aegisName, CancellationToken ct = default) => Task.FromResult(_rows.FirstOrDefault(r => r.NameAegis == aegisName));
        public Task<List<DbMob>> GetByLevelRangeAsync(ushort min, ushort max, CancellationToken ct = default) => Task.FromResult(_rows);
        public Task<List<DbMob>> GetByRaceAsync(string race, CancellationToken ct = default) => Task.FromResult(_rows);
        public Task<List<DbMob>> GetByElementAsync(string element, CancellationToken ct = default) => Task.FromResult(_rows);
        public Task<List<DbMob>> GetBySizeAsync(string size, CancellationToken ct = default) => Task.FromResult(_rows);
        public Task<List<DbMob>> GetAllMvpsAsync(CancellationToken ct = default) => Task.FromResult(_rows);
        public Task<List<DbMob>> SearchByNameAsync(string term, int limit = 50, CancellationToken ct = default) => Task.FromResult(_rows);
        public Task<List<DbMob>> GetByDropItemAsync(string item, CancellationToken ct = default) => Task.FromResult(_rows);
    }

    private sealed class StubMobSkillRepository : IMobSkillDbRepository
    {
        private readonly List<MobSkillDbEntity> _rows;
        public StubMobSkillRepository(params MobSkillDbEntity[] rows) => _rows = rows.ToList();
        public Task<IReadOnlyList<MobSkillDbEntity>> GetAllAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<MobSkillDbEntity>>(_rows);
        public Task<IReadOnlyList<MobSkillDbEntity>> GetByMobIdAsync(int mobId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<MobSkillDbEntity>>(_rows.Where(r => r.MobId == mobId).ToList());
        public Task<int> CountAsync(CancellationToken ct = default) => Task.FromResult(_rows.Count);
    }
}
