using Core.Database.Entities;
using Core.Server.Packets;
using Map.Server;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Pet;
using Map.Server.Pet.PetOps;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Microsoft.Extensions.Logging.Abstractions;
using PetEntity = Map.Server.Entities.PetEntity;

namespace Map.Server.Tests.Pet;

/// <summary>
/// FEATURE-07 — pet egg / hatch lifecycle (CreateEgg / GetEgg / EggSearch / BirthProcess).
/// The catch roll (CatchProcessEnd) was landed by FEATURE-01.
/// </summary>
public class PetLifecycleTests
{
    private const uint PoringClass = 1002;
    private const uint EggItemId = 9001;
    private const string PoringAegis = "PORING";
    private const string EggAegis = "PET_EGG_PORING";

    private static (PetOpsService svc, PlayerEntity pc, MapSessionData session, FakeIntif intif, FakeInventory inv, FakePet pet)
        Build(params InventoryItem[] inv)
    {
        var pc = new PlayerEntity(1, 7, "Owner", Guid.NewGuid(), 1, 50, 50) { Hp = 1, MaxHp = 1 };
        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(sockets.ServerSide, 30000,
            new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        { AccountId = 7, CharacterId = 1, EntityId = pc.Id, Inventory = inv.ToList() };

        var intif = new FakeIntif();
        var fakeInv = new FakeInventory();
        var pet = new FakePet();
        var svc = new PetOpsService(NullLogger<PetOpsService>.Instance,
            new FakeMobDb(), new FakeItems(), intif, rng: new Random(0),
            sessions: new FakeSessions(session), inventory: fakeInv, pet: pet);
        svc.SeedCatalogForTest(new PetDbEntity
        {
            MobAegis = PoringAegis, EggItem = EggAegis, CaptureRate = 10000, IntimacyStart = 250, Fullness = 80,
        });
        svc.InvalidateEggIndexForTest();
        return (svc, pc, session, intif, fakeInv, pet);
    }

    private static InventoryItem Egg(int slot, uint amount = 1) =>
        new() { Id = slot + 1, ServerIndex = slot, NameId = EggItemId, Amount = amount, Identified = true };

    [Fact]
    public void CreateEgg_resolves_class_and_dispatches_PetCreate()
    {
        var (svc, pc, _, intif, _, _) = Build();
        Assert.True(svc.CreateEgg(pc, (int)EggItemId));
        Assert.Equal(1, intif.PetCreateCalls);
        Assert.Equal((int)PoringClass, intif.LastClass);
    }

    [Fact]
    public void CreateEgg_non_pet_item_returns_false()
    {
        var (svc, pc, _, intif, _, _) = Build();
        Assert.False(svc.CreateEgg(pc, itemId: 555)); // not a pet egg
        Assert.Equal(0, intif.PetCreateCalls);
    }

    [Fact]
    public void EggSearch_finds_egg_slot_or_minus1()
    {
        var (svc, pc, _, _, _, _) = Build(Egg(3));
        Assert.Equal(3, svc.EggSearch(pc, (int)EggItemId));
        Assert.Equal(-1, svc.EggSearch(pc, eggId: 8888));
    }

    [Fact]
    public void GetEgg_grants_the_egg_item()
    {
        var (svc, pc, _, _, inv, _) = Build();
        Assert.True(svc.GetEgg(pc, (int)PoringClass, (int)EggItemId, petId: 777));
        Assert.Equal(1, inv.GiveCalls);
        Assert.Equal(EggItemId, inv.LastNameId);
    }

    [Fact]
    public void BirthProcess_hatches_selected_egg_and_consumes_it()
    {
        var (svc, pc, session, _, _, pet) = Build(Egg(2));

        Assert.Equal(0, svc.SelectEgg(pc, 2)); // hatch the egg at slot 2
        Assert.Equal(1, pet.SummonCalls);
        Assert.Equal((int)PoringClass, pet.LastClass);
        Assert.Empty(session.Inventory!); // egg consumed
    }

    [Fact]
    public void BirthProcess_with_no_egg_at_slot_returns_minus1()
    {
        var (svc, pc, _, _, _, pet) = Build();
        Assert.Equal(-1, svc.BirthProcess(pc, 2)); // no egg at that slot
        Assert.Equal(0, pet.SummonCalls);
    }

    // --- fakes ---

    private sealed class FakeSessions(MapSessionData session) : ISessionManagerAccessor
    {
        public MapSessionData? GetByEntityId(EntityId entityId) => session;
    }

    private sealed class FakeInventory : IInventoryService
    {
        public int GiveCalls; public uint LastNameId;
        public Task LoadAsync(MapSessionData session, CancellationToken ct = default) => Task.CompletedTask;
        public void SendInventoryList(MapSessionData session) { }
        public bool GiveItem(MapSessionData session, uint nameId, int amount) { GiveCalls++; LastNameId = nameId; return true; }
        public bool GiveItemWithCards(MapSessionData session, uint nameId, int amount, uint card0, uint card1, uint card2, uint card3) => GiveItem(session, nameId, amount);
    }

    private sealed class FakePet : IPetService
    {
        public int SummonCalls; public int LastClass;
        public PetEntity? Summon(PlayerEntity owner, int petClassId, string petName, int eggItemId = 0, long petId = 0, int intimacy = -1, int hunger = -1, bool renamed = false)
        { SummonCalls++; LastClass = petClassId; return null; }
        public void Recall(PlayerEntity owner) { }
        public void Tick(long nowTick) { }
        public Core.Server.IPC.PetData? SerializeSnapshot(int petId) => null;
        public bool TryGetLivePetId(PlayerEntity owner, out int petId) { petId = 0; return false; }
    }

    private sealed class FakeItems : IItemCatalog
    {
        public int Count => 0;
        public ItemEntity? Get(uint itemId) => new() { Id = itemId };
        public ItemEntity? GetByAegisName(string aegisName) =>
            string.Equals(aegisName, EggAegis, StringComparison.OrdinalIgnoreCase)
                ? new ItemEntity { Id = EggItemId, NameAegis = aegisName } : null;
        public IEnumerable<ItemEntity> All() => Array.Empty<ItemEntity>();
        public void Reload() { }
    }

    private sealed class FakeMobDb : IMobDb
    {
        public int Count => 1;
        public MobDbEntry? Get(int classId) => classId == (int)PoringClass
            ? new MobDbEntry { Id = (int)PoringClass, AegisName = PoringAegis, Name = "Poring" } : null;
        public MobDbEntry? GetByAegisName(string aegisName) =>
            string.Equals(aegisName, PoringAegis, StringComparison.OrdinalIgnoreCase)
                ? new MobDbEntry { Id = (int)PoringClass, AegisName = PoringAegis, Name = "Poring" } : null;
        public IEnumerable<MobDbEntry> All() => Array.Empty<MobDbEntry>();
        public void Reload() { }
    }

    private sealed class FakeIntif : Map.Server.Services.Intif.IIntifService
    {
        public int PetCreateCalls; public int LastClass;
        public int PetCreate(PlayerEntity master, int classId, int nameId, byte rename, int eggItemId, byte intimate, byte hungry, char gender, string petName)
        { PetCreateCalls++; LastClass = classId; return 1; }
        public System.Threading.Tasks.Task<int> PetCreateAsync(PlayerEntity master, int classId, int eggItemId, byte intimate, byte hungry, string petName, System.Threading.CancellationToken ct = default) => System.Threading.Tasks.Task.FromResult(0);
        public System.Threading.Tasks.Task<Core.Server.IPC.PetData?> PetLoadAsync(int petId, int accountId, int charId, System.Threading.CancellationToken ct = default) => System.Threading.Tasks.Task.FromResult<Core.Server.IPC.PetData?>(null);

        // ---- remainder unused ----
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
        public Task AchievementRequestAsync(PlayerEntity pc, CancellationToken ct = default) => Task.CompletedTask;
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
