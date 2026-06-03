using System.Threading;
using System.Threading.Tasks;
using Map.Server.Entities;
using Map.Server.Persistence;
using Map.Server.Pet;
using Map.Server.Services.Intif;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Persistence;

/// <summary>
/// FEATURE-02 — the game-loop save path now fans out to the companion / quest / achievement
/// char-server RPCs that were previously orphaned. Final-save (logout) awaits the async variants
/// so the row lands before teardown; autosave uses the fire-and-forget int wrappers. Phase A
/// covers quest + achievement + pet; pet is included only when one is live.
/// </summary>
public class CompanionSaveFanoutTests
{
    [Fact]
    public async Task Final_save_awaits_quest_achievement_and_pet_when_a_pet_is_live()
    {
        var intif = new RecordingIntif();
        var (svc, pc) = Build(intif, livePetId: 42);

        await svc.SaveCompanionsAsync(pc, finalSave: true);

        Assert.Equal(1, intif.QuestSaveAsyncCalls);
        Assert.Equal(1, intif.AchievementSaveAsyncCalls);
        Assert.Equal(1, intif.SavePetAsyncCalls);
        Assert.Equal(42, intif.LastSavePetId);
        // the fire-and-forget int wrappers are NOT used on the final-save path
        Assert.Equal(0, intif.QuestSaveCalls);
        Assert.Equal(0, intif.AchievementSaveCalls);
        Assert.Equal(0, intif.SavePetCalls);
    }

    [Fact]
    public async Task Final_save_skips_pet_when_none_is_live()
    {
        var intif = new RecordingIntif();
        var (svc, pc) = Build(intif, livePetId: null);

        await svc.SaveCompanionsAsync(pc, finalSave: true);

        Assert.Equal(1, intif.QuestSaveAsyncCalls);
        Assert.Equal(1, intif.AchievementSaveAsyncCalls);
        Assert.Equal(0, intif.SavePetAsyncCalls);
    }

    [Fact]
    public async Task Autosave_uses_the_fire_and_forget_int_wrappers()
    {
        var intif = new RecordingIntif();
        var (svc, pc) = Build(intif, livePetId: 7);

        await svc.SaveCompanionsAsync(pc, finalSave: false);

        Assert.Equal(1, intif.QuestSaveCalls);
        Assert.Equal(1, intif.AchievementSaveCalls);
        Assert.Equal(1, intif.SavePetCalls);
        Assert.Equal(7, intif.LastSavePetId);
        Assert.Equal(0, intif.QuestSaveAsyncCalls); // async variants reserved for final-save
    }

    [Fact]
    public async Task Autosave_skips_pet_when_none_is_live()
    {
        var intif = new RecordingIntif();
        var (svc, pc) = Build(intif, livePetId: null);

        await svc.SaveCompanionsAsync(pc, finalSave: false);

        Assert.Equal(1, intif.QuestSaveCalls);
        Assert.Equal(1, intif.AchievementSaveCalls);
        Assert.Equal(0, intif.SavePetCalls);
    }

    // ---- harness ----

    private static (PlayerStateService svc, PlayerEntity pc) Build(IIntifService intif, int? livePetId)
    {
        var pc = new PlayerEntity(1, 1, "Hero", System.Guid.NewGuid(), 0, 0, 0);
        var pets = new StubPets(livePetId);
        // scopes/entities are unused by SaveCompanionsAsync.
        var svc = new PlayerStateService(scopes: null!, entities: null!, intif, pets, NullLogger<PlayerStateService>.Instance);
        return (svc, pc);
    }

    private sealed class StubPets : IPetService
    {
        private readonly int? _petId;
        public StubPets(int? petId) => _petId = petId;
        public bool TryGetLivePetId(PlayerEntity owner, out int petId)
        {
            petId = _petId ?? 0;
            return _petId.HasValue;
        }
        public PetEntity? Summon(PlayerEntity owner, int petClassId, string petName, int eggItemId = 0) => null;
        public void Recall(PlayerEntity owner) { }
        public void Tick(long nowTick) { }
        public Core.Server.IPC.PetData? SerializeSnapshot(int petId) => null;
    }

    private sealed class RecordingIntif : IIntifService
    {
        public int QuestSaveCalls, AchievementSaveCalls, SavePetCalls;
        public int QuestSaveAsyncCalls, AchievementSaveAsyncCalls, SavePetAsyncCalls;
        public int LastSavePetId;

        public int QuestSave(PlayerEntity pc) { QuestSaveCalls++; return 1; }
        public int AchievementSave(PlayerEntity pc) { AchievementSaveCalls++; return 1; }
        public int SavePet(int petId) { SavePetCalls++; LastSavePetId = petId; return 1; }
        public Task QuestSaveAsync(PlayerEntity pc, CancellationToken ct = default) { QuestSaveAsyncCalls++; return Task.CompletedTask; }
        public Task AchievementSaveAsync(PlayerEntity pc, CancellationToken ct = default) { AchievementSaveAsyncCalls++; return Task.CompletedTask; }
        public Task SavePetAsync(int petId, CancellationToken ct = default) { SavePetAsyncCalls++; LastSavePetId = petId; return Task.CompletedTask; }

        // ---- the rest of the interface (unused by this test) ----
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
        public int AchievementRequest(int charId) => 0;
        public int PetCreate(PlayerEntity master, int classId, int nameId, byte rename, int eggItemId, byte intimate, byte hungry, char gender, string petName) => 0;
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
