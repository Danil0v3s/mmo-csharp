using Map.Server.Entities;
using Map.Server.Services;
using Microsoft.Extensions.Logging;

namespace Map.Server.Services.Intif;

/// <summary>
/// Default <see cref="IIntifService"/>. Every method is a shim that
/// returns 0 / false until its IPC consumer ports — by which point
/// it forwards onto the existing typed *IpcService wrapper. Entry
/// points are here so a rAthena port reads 1:1.
/// </summary>
public sealed class IntifService : IIntifService
{
    private readonly ILogger<IntifService> _logger;
    private readonly ICharServerIpcServiceMail? _mailIpc;
    private readonly ICharServerIpcServiceQuest? _questIpc;
    private readonly Map.Server.Quest.IQuestService? _questService;
    private readonly Map.Server.Achievement.IAchievementService? _achievementService;
    public IntifService(ILogger<IntifService> logger,
        ICharServerIpcServiceMail? mailIpc = null,
        ICharServerIpcServiceQuest? questIpc = null,
        Map.Server.Quest.IQuestService? questService = null,
        Map.Server.Achievement.IAchievementService? achievementService = null)
    {
        _logger = logger;
        _mailIpc = mailIpc;
        _questIpc = questIpc;
        _questService = questService;
        _achievementService = achievementService;
    }

    public int RequestChatName(int charId) => 0;
    public int RequestAccInfo(int callerCharId, int targetCharId) => 0;
    public int Broadcast(string mes, uint colorRgb, byte type) => 0;
    public int Broadcast2(string mes, uint colorRgb, ushort fontType, ushort fontSize, ushort fontAlign, ushort fontY) => 0;
    public int MainMessage(PlayerEntity pc, string text) => 0;
    public int WisMessage(PlayerEntity from, string toName, string text) => 0;
    public int WisMessageToGm(string from, byte minGmLevel, string text) => 0;
    public int SaveRegistry(PlayerEntity pc) => 0;
    public int RequestRegistry(PlayerEntity pc, byte flag) => 0;

    public int CreateParty(PlayerEntity pc, string name, byte item, byte itemDiv) => 0;
    public int RequestPartyInfo(int partyId, int charId) => 0;
    public int AddPartyMember(int partyId, PlayerEntity pc) => 0;
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

    /// <summary>
    /// T5.4a — rAthena <c>intif_Mail_send</c>. Fire-and-forget RPC
    /// onto the char server's MailSend handler. Returns the
    /// synthetic request id (1 on dispatch, 0 when no char server
    /// is connected) — matches the rAthena pattern of returning 1
    /// for "sent to inter server."
    /// </summary>
    public int MailSend(int senderCharId, string toName, string title, string body, int zeny)
    {
        if (_mailIpc == null)
        {
            _logger.LogWarning("intif_Mail_send: char server IPC not wired; dropping mail from {Sender}", senderCharId);
            return 0;
        }
        // Async fire-and-forget — the char server saves the row and
        // notifies the receiver via push when they next log in. We
        // don't await the response; mail send is best-effort from
        // the map's perspective.
        _ = _mailIpc.MailSendAsync(
            senderAccountId: 0, // account id resolution lives char-side
            senderCharacterId: senderCharId,
            senderName: string.Empty, // char server backfills from db
            receiverAccountId: 0,
            receiverCharacterId: 0,
            receiverName: toName ?? string.Empty,
            title: title ?? string.Empty,
            body: body ?? string.Empty,
            zeny: zeny,
            attachment: Array.Empty<byte>());
        _logger.LogDebug("intif_Mail_send sender={Sender} to={To} title={Title}",
            senderCharId, toName, title);
        return 1;
    }

    /// <summary>
    /// T5.4a — rAthena <c>intif_Mail_return</c>. Fire-and-forget
    /// MailReturn RPC.
    /// </summary>
    public int MailReturn(int charId, int mailId)
    {
        if (_mailIpc == null) return 0;
        _ = _mailIpc.MailReturnAsync(accountId: 0, characterId: charId, mailId: mailId);
        return 1;
    }

    public int AuctionRequestList(int charId, byte type, int price, string search, byte page) => 0;
    public int AuctionRegister(int charId, byte type, int sellerCharId, string sellerName, int now, int hours, int priceStart, int priceBuyNow, int itemId, byte refine, byte attribute, int identify, int amount) => 0;
    public int AuctionCancel(int charId, uint auctionId) => 0;
    public int AuctionClose(int charId, uint auctionId) => 0;
    public int AuctionBid(int charId, uint auctionId, int bid, string bidder) => 0;

    /// <summary>
    /// T5.4b — rAthena <c>intif_quest_save</c> (intif.cpp:2050).
    /// T7.1 — now dispatches a real per-objective snapshot via
    /// <see cref="Map.Server.Quest.IQuestService.SnapshotFor"/>
    /// (falls back to empty when no QuestService is wired, matching
    /// the char-side DELETE-then-INSERT pattern). Returns 1 on
    /// dispatch, 0 when no char server IPC is wired.
    /// </summary>
    public int QuestSave(PlayerEntity pc)
    {
        if (_questIpc == null) return 0;
        var snapshot = _questService?.SnapshotFor(pc)
            ?? (IReadOnlyList<Core.Server.IPC.QuestEntryData>)Array.Empty<Core.Server.IPC.QuestEntryData>();
        _ = _questIpc.QuestSaveAsync(pc.CharacterId, quests: snapshot);
        return 1;
    }

    /// <summary>
    /// T5.4b — rAthena <c>intif_request_questlog</c>. Issues the
    /// async load; the response feeds into QuestService.Hydrate at
    /// session enter.
    /// </summary>
    public int QuestRequest(int charId)
    {
        if (_questIpc == null) return 0;
        _ = _questIpc.QuestLoadAsync(charId);
        return 1;
    }

    /// <summary>
    /// T5.4c — rAthena <c>intif_achievement_save</c> (intif.cpp:2125).
    /// T7.1 — now dispatches a real per-achievement snapshot via
    /// <see cref="Map.Server.Achievement.IAchievementService.SnapshotFor"/>.
    /// </summary>
    public int AchievementSave(PlayerEntity pc)
    {
        if (_questIpc == null) return 0;
        var snapshot = _achievementService?.SnapshotFor(pc)
            ?? (IReadOnlyList<Core.Server.IPC.AchievementEntryData>)Array.Empty<Core.Server.IPC.AchievementEntryData>();
        _ = _questIpc.AchievementSaveAsync(pc.CharacterId, achievements: snapshot);
        return 1;
    }

    /// <summary>
    /// T5.4c — rAthena <c>intif_request_achievementlist</c>.
    /// </summary>
    public int AchievementRequest(int charId)
    {
        if (_questIpc == null) return 0;
        _ = _questIpc.AchievementLoadAsync(charId);
        return 1;
    }

    public int PetCreate(PlayerEntity master, int classId, int nameId, byte rename, int eggItemId, byte intimate, byte hungry, char gender, string petName) => 0;
    public int RequestPetInfo(int petId, int accountId, byte flag) => 0;
    public int SavePet(int petId) => 0;
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
