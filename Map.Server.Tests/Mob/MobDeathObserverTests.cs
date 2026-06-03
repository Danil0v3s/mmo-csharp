using Core.Database.Entities;
using Map.Server.Achievement;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Pet.PetOps;
using Map.Server.Quest;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Mob;

/// <summary>
/// FEATURE-01 — the mob-death observer hub. Verifies the quest / achievement / pet-catch / MVP
/// fan-out (rAthena mob_dead steps 4–7) fires the right state mutations for the right PCs.
/// </summary>
public class MobDeathObserverTests
{
    private const int PoporingClass = 1031;
    private const string PoporingAegis = "POPORING";

    // --- Quest ---

    [Fact]
    public void Quest_increments_for_matching_mob_and_contributor()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1);
        ctx.Quest.SeedCatalogForTest(new QuestDbEntity { QuestId = 60001, Mob1 = PoporingAegis, Count1 = 3 });
        pc.QuestLog.Add(new QuestEntry { QuestId = 60001, State = 1, Counts = new int[1] });

        ctx.Observer.OnMobDead(ctx.Mob(), pc, ctx.DmgLog(pc));

        Assert.Equal(1, pc.QuestLog[0].Counts[0]);
        Assert.Equal(1, pc.QuestLog[0].State); // still active (1 of 3)
    }

    [Fact]
    public void Quest_completes_when_last_objective_met()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1);
        ctx.Quest.SeedCatalogForTest(new QuestDbEntity { QuestId = 60001, Mob1 = PoporingAegis, Count1 = 1 });
        pc.QuestLog.Add(new QuestEntry { QuestId = 60001, State = 1, Counts = new int[1] });

        ctx.Observer.OnMobDead(ctx.Mob(), pc, ctx.DmgLog(pc));

        Assert.Equal(1, pc.QuestLog[0].Counts[0]);
        Assert.Equal(2, pc.QuestLog[0].State); // Q_COMPLETE
    }

    [Fact]
    public void Quest_no_increment_for_nonmatching_mob()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1);
        ctx.Quest.SeedCatalogForTest(new QuestDbEntity { QuestId = 60001, Mob1 = "PORING", Count1 = 3 });
        pc.QuestLog.Add(new QuestEntry { QuestId = 60001, State = 1, Counts = new int[1] });

        ctx.Observer.OnMobDead(ctx.Mob(), pc, ctx.DmgLog(pc));

        Assert.Equal(0, pc.QuestLog[0].Counts[0]);
    }

    [Fact]
    public void Quest_credits_only_damage_contributors()
    {
        var ctx = Build();
        var hitter = ctx.AddPlayer(1);
        var bystander = ctx.AddPlayer(2);
        ctx.Quest.SeedCatalogForTest(new QuestDbEntity { QuestId = 60001, Mob1 = PoporingAegis, Count1 = 3 });
        hitter.QuestLog.Add(new QuestEntry { QuestId = 60001, State = 1, Counts = new int[1] });
        bystander.QuestLog.Add(new QuestEntry { QuestId = 60001, State = 1, Counts = new int[1] });

        // Only `hitter` is in the damage log / is the killer.
        ctx.Observer.OnMobDead(ctx.Mob(), hitter, ctx.DmgLog(hitter));

        Assert.Equal(1, hitter.QuestLog[0].Counts[0]);
        Assert.Equal(0, bystander.QuestLog[0].Counts[0]);
    }

    // --- Achievement ---

    [Fact]
    public void Achievement_increments_for_each_contributor()
    {
        var ctx = Build();
        var a = ctx.AddPlayer(1);
        var b = ctx.AddPlayer(2);
        // @id token avoids needing a mob_db for name resolution.
        ctx.Achievement.SeedCatalogForTest(new AchievementDbEntity
        {
            AchievementId = 70001, GroupName = "AG_BATTLE", Targets = $"@id={PoporingClass}:5", Score = 10,
        });

        ctx.Observer.OnMobDead(ctx.Mob(), a, ctx.DmgLog(a, b));

        Assert.Equal(1, a.AchievementLog.Single().Counts[0]);
        Assert.Equal(1, b.AchievementLog.Single().Counts[0]);
    }

    [Fact]
    public void Achievement_completes_and_ignores_unrelated_mob()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1);
        ctx.Achievement.SeedCatalogForTest(new AchievementDbEntity
        {
            AchievementId = 70002, GroupName = "AG_BATTLE", Targets = "@id=9999:1", Score = 10,
        });

        ctx.Observer.OnMobDead(ctx.Mob(), pc, ctx.DmgLog(pc)); // killed 1031, achievement wants 9999

        Assert.Empty(pc.AchievementLog); // no entry created for an unrelated mob
    }

    [Fact]
    public void Achievement_resolves_aegis_name_targets_via_mobdb()
    {
        // The aegis-name target path resolves through IMobDb (the @id path is covered above).
        var ach = new AchievementService(NullLogger<AchievementService>.Instance, new FakeMobDb());
        ach.SeedCatalogForTest(new AchievementDbEntity
        {
            AchievementId = 70003, GroupName = "AG_BATTLE", Targets = $"{PoporingAegis}:2", Score = 5,
        });
        var pc = new PlayerEntity(1, 1, "P1", Guid.NewGuid(), 1, 50, 50) { Hp = 1000, MaxHp = 1000 };

        Assert.True(ach.MobExists(PoporingClass));
        ach.UpdateObjective(pc, (byte)AchievementGroup.Battle, 0, PoporingClass);

        Assert.Equal(1, pc.AchievementLog.Single().Counts[0]);
    }

    // NOTE: pet capture is no longer a death event (it was a parity bug). rAthena rolls the catch
    // when the player clicks the LIVE mob (CZ_TRYCAPTURE_MONSTER) — see PetCaptureTests.

    // --- MVP ---

    [Fact]
    public void Mvp_awards_exp_and_one_drop_to_top_damager()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1);
        var mob = ctx.Mob(mvpExp: 5000, mvpDrop: ("WHITE_POTION", 10000));
        mob.DmgList.Record(pc.Id, 999); // top damager

        ctx.Observer.OnMobDead(mob, pc, mob.DmgList.Snapshot());

        Assert.Equal(1, ctx.Exp.GainExpCalls);
        Assert.Equal(5000, ctx.Exp.LastBaseExp);
        Assert.Equal(1, ctx.Drops.DropCalls);
        Assert.True(ctx.Drops.LastWasMvp);
    }

    [Fact]
    public void Non_mvp_mob_awards_no_mvp_exp_or_drop()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1);
        var mob = ctx.Mob(); // MvpExp 0, no MVP drops

        ctx.Observer.OnMobDead(mob, pc, ctx.DmgLog(pc));

        Assert.Equal(0, ctx.Exp.GainExpCalls);
        Assert.Equal(0, ctx.Drops.DropCalls);
    }

    // --- scaffolding ---

    private static TestContext Build()
    {
        const string mapName = "death_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorld(map);
        var entities = new EntityRegistry(world);
        var mapId = (uint)mapName.GetHashCode();

        var quest = new QuestService(NullLogger<QuestService>.Instance);
        var achievement = new AchievementService(NullLogger<AchievementService>.Instance);
        var mobDb = new FakeMobDb();
        var items = new FakeItems();
        var intif = new RecordingIntif();
        var exp = new FakeExp();
        var drops = new FakeDrops();

        var observer = new MobDeathObserver(entities, quest, achievement,
            NullLogger<MobDeathObserver>.Instance, exp, drops, items, players: null, sessions: null);

        return new TestContext(observer, quest, achievement, intif, exp, drops, entities, mapId);
    }

    private sealed record TestContext(
        MobDeathObserver Observer, QuestService Quest, AchievementService Achievement,
        RecordingIntif Intif, FakeExp Exp, FakeDrops Drops, EntityRegistry Entities, uint MapId)
    {
        private int _nextId = 5000;

        public PlayerEntity AddPlayer(int charId)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, 50, 50);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
        }

        public MobEntity Mob(long mvpExp = 0, (string Item, int Rate)? mvpDrop = null)
        {
            var db = new MobDbEntry
            {
                Id = PoporingClass, AegisName = PoporingAegis, Name = "Poporing", Hp = 500, Level = 20,
                MvpExp = mvpExp,
                MvpDrops = mvpDrop is { } d
                    ? new[] { new MobDrop(d.Item, d.Rate) }
                    : Array.Empty<MobDrop>(),
            };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = PoporingClass };
            var mob = new MobEntity(new EntityId(_nextId++), db, origin, MapId, 50, 50);
            Entities.Add(mob);
            return mob;
        }

        /// <summary>A damage-log snapshot crediting each given PC.</summary>
        public IReadOnlyList<MobDmgList.DmgEntry> DmgLog(params PlayerEntity[] pcs)
        {
            var list = new List<MobDmgList.DmgEntry>();
            foreach (var pc in pcs) list.Add(new MobDmgList.DmgEntry(pc.Id, 100));
            return list;
        }
    }

    // --- fakes ---

    private sealed class FakeExp : IExpService
    {
        public int GainExpCalls; public long LastBaseExp;
        public bool GainExp(PlayerEntity player, long baseExp, long jobExp, int? mobLevel = null)
        { GainExpCalls++; LastBaseExp = baseExp; return false; }
        public (long BaseLost, long JobLost) LoseExp(PlayerEntity player, long baseExp, long jobExp) => (0, 0);
        public void OnBaseLevelChanged(PlayerEntity player) { }
    }

    private sealed class FakeDrops : IItemDropService
    {
        public int DropCalls; public bool LastWasMvp;
        public EntityId DropOnFloor(uint mapId, short x, short y, int itemId, short amount,
            byte subX = 0, byte subY = 0, bool identified = true,
            int ownerCharId = 0, int ownerPartyId = 0, int ownerGuildId = 0, bool isMvpDrop = false)
        { DropCalls++; LastWasMvp = isMvpDrop; return new EntityId(1); }
        public IItemDropService.PickupResult TryPickup(PlayerEntity picker, EntityId itemEntityId, out FloorItemEntity? item)
        { item = null; return IItemDropService.PickupResult.ItemNotFound; }
        public void Tick() { }
    }

    private sealed class FakeItems : IItemCatalog
    {
        public int Count => 0;
        public ItemEntity? Get(uint itemId) => new ItemEntity { Id = itemId };
        public ItemEntity? GetByAegisName(string aegisName) => new ItemEntity { Id = 9001, NameAegis = aegisName };
        public IEnumerable<ItemEntity> All() => Array.Empty<ItemEntity>();
        public void Reload() { }
    }

    private sealed class FakeMobDb : IMobDb
    {
        public int Count => 1;
        public MobDbEntry? Get(int classId) => classId == PoporingClass
            ? new MobDbEntry { Id = PoporingClass, AegisName = PoporingAegis, Name = "Poporing" } : null;
        public MobDbEntry? GetByAegisName(string aegisName) =>
            string.Equals(aegisName, PoporingAegis, StringComparison.OrdinalIgnoreCase)
                ? new MobDbEntry { Id = PoporingClass, AegisName = PoporingAegis, Name = "Poporing" } : null;
        public IEnumerable<MobDbEntry> All() => Array.Empty<MobDbEntry>();
        public void Reload() { }
    }

    private sealed class StubWorld : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorld(params MapData[] maps) => _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }

    private sealed class RecordingIntif : Map.Server.Services.Intif.IIntifService
    {
        public int PetCreateCalls; public int LastPetClass;
        public int PetCreate(PlayerEntity master, int classId, int nameId, byte rename, int eggItemId, byte intimate, byte hungry, char gender, string petName)
        { PetCreateCalls++; LastPetClass = classId; return 1; }

        // ---- remainder of the interface (unused) ----
        public int QuestSave(PlayerEntity pc) => 0;
        public int AchievementSave(PlayerEntity pc) => 0;
        public int SavePet(int petId) => 0;
        public Task QuestSaveAsync(PlayerEntity pc, CancellationToken ct = default) => Task.CompletedTask;
        public Task AchievementSaveAsync(PlayerEntity pc, CancellationToken ct = default) => Task.CompletedTask;
        public Task SavePetAsync(int petId, CancellationToken ct = default) => Task.CompletedTask;
        public int RequestChatName(int charId) => 0;
        public int RequestAccInfo(int callerCharId, int targetCharId) => 0;
        public int Broadcast(string mes, uint colorRgb, byte type) => 0;
        public int Broadcast2(string mes, uint colorRgb, ushort fontType, ushort fontSize, ushort fontAlign, ushort fontY) => 0;
        public int MainMessage(PlayerEntity pc, string text) => 0;
        public int WisMessage(PlayerEntity from, string toName, string text) => 0;
        public int WisMessageToGm(string from, byte minGmLevel, string text) => 0;
        public int SaveRegistry(PlayerEntity pc) => 0;
        public int RequestRegistry(PlayerEntity pc, byte flag) => 0;
        public int AddPartyMember(int partyId, PlayerEntity pc) => 0;
        public int CreateParty(PlayerEntity pc, string name, byte item, byte itemDiv) => 0;
        public int RequestPartyInfo(int partyId, int charId) => 0;
        public int ChangePartyLeader(int partyId, int accountId, int charId) => 0;
        public int PartyChangeOption(int partyId, int accountId, int exp, int item, int flag) => 0;
        public int LeaveParty(int partyId, int accountId, int charId) => 0;
        public int PartyChangemap(PlayerEntity pc, bool online) => 0;
        public int BreakParty(int partyId) => 0;
        public int PartyMessage(int partyId, int accountId, string text) => 0;
        public bool GuildCreate(string name, PlayerEntity master) => false;
        public int GuildRequestInfo(int guildId) => 0;
        public int GuildAddMember(int guildId, PlayerEntity pc) => 0;
        public int GuildLeave(int guildId, int accountId, int charId, byte flag, string mes) => 0;
        public int GuildExpulsion(int guildId, int accountId, int charId, string mes) => 0;
        public int GuildBreak(int guildId) => 0;
        public int GuildMessage(int guildId, int accountId, string mes) => 0;
        public int GuildEmblem(int guildId, byte[] emblem) => 0;
        public int GuildSavePosition(int guildId, byte idx, int mode, int exp_mode, string name) => 0;
        public int GuildSetSkill(int guildId, ushort skillId, ushort skillLevel) => 0;
        public int GuildAllianceAck(int guildId, int allyId, int accountId, int charId, int flag, string mes) => 0;
        public int GuildAddCastle(int castleId, int guildId) => 0;
        public int MailRequestInbox(int charId, byte flag) => 0;
        public int MailRead(int mailId) => 0;
        public int MailGetAttach(int charId, int mailId, byte flag) => 0;
        public int MailDelete(int charId, int mailId) => 0;
        public int MailSend(int senderCharId, string toName, string title, string body, int zeny) => 0;
        public int MailReturn(int charId, int mailId) => 0;
        public int AuctionRequestList(int charId, byte type, int price, string search, byte page) => 0;
        public int AuctionRegister(int charId, byte type, int sellerCharId, string sellerName, int now, int hours, int priceStart, int priceBuyNow, int itemId, byte refine, byte attribute, int identify, int amount) => 0;
        public int AuctionCancel(int charId, uint auctionId) => 0;
        public int AuctionClose(int charId, uint auctionId) => 0;
        public int AuctionBid(int charId, uint auctionId, int bid, string bidder) => 0;
        public int QuestRequest(int charId) => 0;
        public System.Threading.Tasks.Task QuestRequestAsync(Map.Server.Entities.PlayerEntity pc, System.Threading.CancellationToken ct = default) => System.Threading.Tasks.Task.CompletedTask;
        public int AchievementRequest(int charId) => 0;
        public int RequestPetInfo(int petId, int accountId, byte flag) => 0;
        public int DeletePet(int petId) => 0;
        public int HomunculusCreate(int accountId, byte[] data) => 0;
        public int HomunculusRequest(int accountId, int homunId) => 0;
        public int HomunculusSave(byte[] data) => 0;
        public int HomunculusDelete(int homunId) => 0;
        public int MercenaryCreate(byte[] data) => 0;
        public int MercenaryRequest(int accountId, int mercId) => 0;
        public int MercenarySave(byte[] data) => 0;
        public int MercenaryDelete(int mercId) => 0;
        public int ClanMessage(int clanId, int charId, string text) => 0;
        public int RequestAccountStorage(int accountId) => 0;
        public int SaveAccountStorage(int accountId, byte[] data) => 0;
        public int RequestGuildStorage(int charId, int guildId) => 0;
        public int SaveGuildStorage(int charId, int guildId, byte[] data) => 0;
        public int CreateBg(int mapIndex, byte side) => 0;
        public int BgRecord(int bgId, int charId, byte score) => 0;
        public int ElementalCreate(byte[] data) => 0;
        public int ElementalRequest(int accountId, int eleId) => 0;
        public int ElementalSave(byte[] data) => 0;
        public int ElementalDelete(int eleId) => 0;
        public int RequestMapreg() => 0;
        public int SaveMapreg(byte[] data) => 0;
        public bool CheckConnection() => true;
        public void Init() { }
        public void Final() { }
    }
}
