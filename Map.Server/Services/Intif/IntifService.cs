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
    private readonly ICharServerIpcServicePet? _petIpc;
    private readonly ICharServerIpcServiceHomunculus? _homunIpc;
    private readonly ICharServerIpcServiceMercenary? _mercIpc;
    private readonly ICharServerIpcServiceElemental? _elemIpc;
    private readonly ICharServerIpcServiceStorage? _storageIpc;
    private readonly ICharServerIpcServiceAuction? _auctionIpc;
    private readonly ICharServerIpcServiceMapreg? _mapregIpc;
    private readonly Map.Server.Quest.IQuestService? _questService;
    private readonly Map.Server.Achievement.IAchievementService? _achievementService;
    private readonly Map.Server.Pet.IPetService? _petService;
    private readonly Map.Server.Homunculus.IHomunculusService? _homunService;
    private readonly Map.Server.Mercenary.IMercenaryService? _mercService;
    private readonly Map.Server.Elemental.IElementalService? _elemService;
    private readonly ICharServerIpcServiceParty? _partyIpc;
    private readonly Map.Server.Party.IPartyService? _partyService;
    private readonly Map.Server.World.IMapWorldRegistry? _world;
    public IntifService(ILogger<IntifService> logger,
        ICharServerIpcServiceMail? mailIpc = null,
        ICharServerIpcServiceQuest? questIpc = null,
        ICharServerIpcServicePet? petIpc = null,
        ICharServerIpcServiceHomunculus? homunIpc = null,
        ICharServerIpcServiceMercenary? mercIpc = null,
        ICharServerIpcServiceElemental? elemIpc = null,
        ICharServerIpcServiceStorage? storageIpc = null,
        ICharServerIpcServiceAuction? auctionIpc = null,
        ICharServerIpcServiceMapreg? mapregIpc = null,
        ICharServerIpcServiceParty? partyIpc = null,
        Map.Server.Quest.IQuestService? questService = null,
        Map.Server.Achievement.IAchievementService? achievementService = null,
        Map.Server.Pet.IPetService? petService = null,
        Map.Server.Homunculus.IHomunculusService? homunService = null,
        Map.Server.Mercenary.IMercenaryService? mercService = null,
        Map.Server.Elemental.IElementalService? elemService = null,
        Map.Server.Party.IPartyService? partyService = null,
        Map.Server.World.IMapWorldRegistry? world = null)
    {
        _logger = logger;
        _mailIpc = mailIpc;
        _questIpc = questIpc;
        _petIpc = petIpc;
        _homunIpc = homunIpc;
        _mercIpc = mercIpc;
        _elemIpc = elemIpc;
        _storageIpc = storageIpc;
        _auctionIpc = auctionIpc;
        _mapregIpc = mapregIpc;
        _partyIpc = partyIpc;
        _questService = questService;
        _achievementService = achievementService;
        _petService = petService;
        _homunService = homunService;
        _mercService = mercService;
        _elemService = elemService;
        _partyService = partyService;
        _world = world;
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

    /// <summary>Wave 87 — rAthena <c>intif_create_party</c>. Dispatches typed PartyCreate RPC.</summary>
    public int CreateParty(PlayerEntity pc, string name, byte item, byte itemDiv)
    {
        if (_partyIpc == null) return 0;
        _ = _partyIpc.PartyCreateAsync(name ?? string.Empty, item, itemDiv,
            pc.AccountId, pc.CharacterId, pc.Name ?? string.Empty,
            pc.ClassId, ResolveMapName(pc.MapId), (uint)pc.Level);
        return 1;
    }

    /// <summary>Wave 87 — rAthena <c>intif_request_partyinfo</c>. Async pull → hydrates map-side cache.</summary>
    public int RequestPartyInfo(int partyId, int charId)
    {
        if (_partyService == null) return 0;
        _partyService.Hydrate(partyId, charId);
        return 1;
    }

    /// <summary>Wave 87 — rAthena <c>intif_party_addmember</c>.</summary>
    public int AddPartyMember(int partyId, PlayerEntity pc)
    {
        if (_partyIpc == null) return 0;
        _ = _partyIpc.PartyAddMemberAsync(partyId, pc.AccountId, pc.CharacterId,
            pc.Name ?? string.Empty, pc.ClassId, ResolveMapName(pc.MapId), (uint)pc.Level);
        return 1;
    }

    /// <summary>Wave 87 — rAthena <c>intif_party_leaderchange</c>.</summary>
    public int ChangePartyLeader(int partyId, int accountId, int charId)
    {
        if (_partyIpc == null) return 0;
        _ = _partyIpc.PartyLeaderChangeAsync(partyId, accountId, charId);
        return 1;
    }

    /// <summary>Wave 87 — rAthena <c>intif_party_changeoption</c>.</summary>
    public int PartyChangeOption(int partyId, int accountId, int exp, int item, int flag)
    {
        if (_partyIpc == null) return 0;
        _ = _partyIpc.PartyChangeOptionAsync(partyId, accountId, exp, item);
        return 1;
    }

    /// <summary>Wave 87 — rAthena <c>intif_party_leave</c>. Covers party_removemember / removemember2 / leave.</summary>
    public int LeaveParty(int partyId, int accountId, int charId)
    {
        if (_partyIpc == null) return 0;
        _ = _partyIpc.PartyLeaveAsync(partyId, accountId, charId, string.Empty, 0);
        return 1;
    }

    /// <summary>Wave 87 — rAthena <c>intif_party_changemap</c>. Updates cache then dispatches RPC.</summary>
    public int PartyChangemap(PlayerEntity pc, bool online)
    {
        if (pc.PartyId <= 0) return 0;
        var mapName = ResolveMapName(pc.MapId);
        _partyService?.UpdateMemberMap(pc.PartyId, pc.CharacterId, mapName, (uint)pc.Level, online);
        if (_partyIpc == null) return 0;
        _ = _partyIpc.PartyChangeMapAsync(pc.PartyId, pc.AccountId, pc.CharacterId, online, (uint)pc.Level, mapName);
        return 1;
    }

    /// <summary>Wave 87 — rAthena <c>intif_break_party</c>. Pre-drops cached entry.</summary>
    public int BreakParty(int partyId)
    {
        _partyService?.Forget(partyId);
        if (_partyIpc == null) return 0;
        _ = _partyIpc.PartyBreakAsync(partyId);
        return 1;
    }

    /// <summary>Wave 87 — rAthena <c>intif_party_message</c>. PartyChatHandler drives this too.</summary>
    public int PartyMessage(int partyId, int accountId, string text)
    {
        if (_partyIpc == null) return 0;
        _ = _partyIpc.PartyMessageAsync(partyId, accountId, text ?? string.Empty);
        return 1;
    }

    private string ResolveMapName(uint mapId)
    {
        if (_world == null) return string.Empty;
        foreach (var m in _world.All)
        {
            if ((uint)m.Name.GetHashCode() == mapId) return m.Name;
        }
        return string.Empty;
    }

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

    /// <summary>
    /// T7.6 — rAthena <c>intif_Mail_requestinbox</c> (intif.cpp:1572).
    /// Dispatches the async inbox-pull through
    /// <c>ICharServerIpcServiceMail.MailRequestInboxAsync</c>. The char
    /// side responds with the full mail list; the response feeds the
    /// MailInboxService when the player opens the mail UI. Returns 1
    /// on dispatch, 0 when no char server IPC is wired (matches the
    /// rAthena <c>intif_check_connection</c> gate).
    /// </summary>
    public int MailRequestInbox(int charId, byte flag)
    {
        if (_mailIpc == null) return 0;
        // rAthena passes char_id + a "flag" byte (0 = open / 1 = refresh);
        // the C# RPC normalizes both into a single inbox-fetch (the
        // response is the full list either way, so the client distinction
        // is purely cosmetic).
        _ = _mailIpc.MailRequestInboxAsync(accountId: 0, characterId: charId);
        return 1;
    }

    /// <summary>
    /// T7.6 — rAthena <c>intif_Mail_read</c> (intif.cpp:1601). Marks
    /// the mail row as READ on the char side.
    /// </summary>
    public int MailRead(int mailId)
    {
        if (_mailIpc == null) return 0;
        _ = _mailIpc.MailReadAsync(accountId: 0, characterId: 0, mailId: mailId);
        return 1;
    }

    /// <summary>
    /// T7.6 — rAthena <c>intif_Mail_getattach</c> (intif.cpp:1617).
    /// Pulls the attachment payload + clears the row's attachment +
    /// zeny on the char side (delete-on-grab semantics).
    /// </summary>
    public int MailGetAttach(int charId, int mailId, byte flag)
    {
        if (_mailIpc == null) return 0;
        _ = _mailIpc.MailGetAttachmentAsync(accountId: 0, characterId: charId, mailId: mailId);
        return 1;
    }

    /// <summary>
    /// T7.6 — rAthena <c>intif_Mail_delete</c> (intif.cpp:1647). Hard-
    /// deletes the mail row (cascades to attachments via FK).
    /// </summary>
    public int MailDelete(int charId, int mailId)
    {
        if (_mailIpc == null) return 0;
        _ = _mailIpc.MailDeleteAsync(accountId: 0, characterId: charId, mailId: mailId);
        return 1;
    }

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

    /// <summary>
    /// T7.5 — rAthena <c>intif_Auction_requestlist</c> (intif.cpp:2569).
    /// Dispatches the filtered list pull through
    /// <see cref="ICharServerIpcServiceAuction.AuctionRequestListAsync"/>.
    /// </summary>
    public int AuctionRequestList(int charId, byte type, int price, string search, byte page)
    {
        if (_auctionIpc == null) return 0;
        _ = _auctionIpc.AuctionRequestListAsync(
            characterId: charId,
            type: type,
            price: price,
            searchText: search ?? string.Empty,
            page: page);
        return 1;
    }

    /// <summary>
    /// T7.5 — rAthena <c>intif_Auction_register</c> (intif.cpp:2595).
    /// Packs the per-auction args into <see cref="Core.Server.IPC.AuctionData"/>
    /// and dispatches. Char-side allocates the auction_id and writes the row.
    /// </summary>
    public int AuctionRegister(int charId, byte type, int sellerCharId, string sellerName,
        int now, int hours, int priceStart, int priceBuyNow, int itemId,
        byte refine, byte attribute, int identify, int amount)
    {
        if (_auctionIpc == null) return 0;
        var auction = new Core.Server.IPC.AuctionData
        {
            SellerCharacterId = sellerCharId,
            SellerName = sellerName ?? string.Empty,
            ItemId = itemId,
            ItemType = type,
            Refine = refine,
            Attribute = attribute,
            Price = priceStart,
            BuyNow = priceBuyNow,
            Hours = hours,
            // Char-side computes EndTimeUnix = now + hours*3600.
        };
        _ = _auctionIpc.AuctionRegisterAsync(auction);
        return 1;
    }

    /// <summary>
    /// T7.5 — rAthena <c>intif_Auction_cancel</c> (intif.cpp:2628). Seller-
    /// initiated cancel; char side issues a refund-mail to any current bidder.
    /// </summary>
    public int AuctionCancel(int charId, uint auctionId)
    {
        if (_auctionIpc == null) return 0;
        _ = _auctionIpc.AuctionCancelAsync(characterId: charId, auctionId: (long)auctionId);
        return 1;
    }

    /// <summary>
    /// T7.5 — rAthena <c>intif_Auction_close</c> (intif.cpp:2643). Buyer-
    /// initiated "buy now" close; char side mails item to buyer + zeny to
    /// seller.
    /// </summary>
    public int AuctionClose(int charId, uint auctionId)
    {
        if (_auctionIpc == null) return 0;
        _ = _auctionIpc.AuctionCloseAsync(characterId: charId, auctionId: (long)auctionId);
        return 1;
    }

    /// <summary>
    /// T7.5 — rAthena <c>intif_Auction_bid</c> (intif.cpp:2658). Char side
    /// validates against current high bid + refunds outbid prior bidder
    /// via mail (P1 fix).
    /// </summary>
    public int AuctionBid(int charId, uint auctionId, int bid, string bidder)
    {
        if (_auctionIpc == null) return 0;
        _ = _auctionIpc.AuctionBidAsync(
            characterId: charId,
            bidderName: bidder ?? string.Empty,
            auctionId: (long)auctionId,
            bid: bid);
        return 1;
    }

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

    /// <summary>
    /// T7.2 — rAthena <c>intif_create_pet</c> (intif.cpp:1342). Asks the
    /// char server to insert a new pet row + assign a pet_id. The
    /// response feeds <c>PetService.Hydrate</c> on the map side when the
    /// PetLoad path lands.
    /// </summary>
    public int PetCreate(PlayerEntity master, int classId, int nameId, byte rename, int eggItemId, byte intimate, byte hungry, char gender, string petName)
    {
        if (_petIpc == null) return 0;
        _ = _petIpc.PetCreateAsync(
            accountId: master.AccountId,
            characterId: master.CharacterId,
            classId: classId,
            level: 1, // rAthena pet_data_init starts at lv 1
            eggItemId: eggItemId,
            equipItemId: 0,
            intimacy: intimate,
            hungry: hungry,
            renameFlag: rename,
            incubate: false,
            name: petName ?? string.Empty);
        return 1;
    }

    /// <summary>
    /// T7.2 — rAthena <c>intif_request_petdata</c> (intif.cpp:1370).
    /// Pulls a saved pet by id (after egg-hatch / login). Char-side
    /// looks up the row; map side will hydrate via PetService when
    /// the response handler lands.
    /// </summary>
    public int RequestPetInfo(int petId, int accountId, byte flag)
    {
        if (_petIpc == null) return 0;
        _ = _petIpc.PetLoadAsync(accountId: accountId, characterId: 0, petId: petId);
        return 1;
    }

    /// <summary>
    /// T7.2 — rAthena <c>intif_save_petdata</c> (intif.cpp:1393). Snapshots
    /// the live <see cref="Map.Server.Entities.PetEntity"/> by id via
    /// <see cref="Map.Server.Pet.IPetService.SerializeSnapshot"/> and
    /// dispatches <c>PetSaveAsync</c>.
    /// </summary>
    public int SavePet(int petId)
    {
        if (_petIpc == null) return 0;
        var snapshot = _petService?.SerializeSnapshot(petId);
        if (snapshot == null)
        {
            _logger.LogDebug("intif_save_petdata: no live pet for id {PetId}", petId);
            return 0;
        }
        _ = _petIpc.PetSaveAsync(accountId: snapshot.AccountId, pet: snapshot);
        return 1;
    }

    /// <summary>
    /// T7.2 — rAthena <c>intif_delete_petdata</c> (intif.cpp:1417). Hard-
    /// deletes the pet row on the char side (cascades to pet_skill).
    /// </summary>
    public int DeletePet(int petId)
    {
        if (_petIpc == null) return 0;
        _ = _petIpc.PetDeleteAsync(petId: petId);
        return 1;
    }
    /// <summary>
    /// T7.3 — rAthena <c>intif_homunculus_create</c> (intif.cpp:1771).
    /// Legacy <paramref name="data"/> byte[] carries the rAthena
    /// homunculus struct; the C# proto carries it as
    /// <see cref="Core.Server.IPC.HomunculusData.Payload"/>. Char-side
    /// decoder owns interpretation.
    /// </summary>
    public int HomunculusCreate(int accountId, byte[] data)
    {
        if (_homunIpc == null) return 0;
        _ = _homunIpc.HomunculusCreateAsync(accountId,
            new Core.Server.IPC.HomunculusData
            {
                Payload = Google.Protobuf.ByteString.CopyFrom(data ?? Array.Empty<byte>()),
            });
        return 1;
    }

    /// <summary>
    /// T7.3 — rAthena <c>intif_homunculus_requestload</c> (intif.cpp:1793).
    /// </summary>
    public int HomunculusRequest(int accountId, int homunId)
    {
        if (_homunIpc == null) return 0;
        _ = _homunIpc.HomunculusLoadAsync(accountId, homunId);
        return 1;
    }

    /// <summary>
    /// T7.3 — rAthena <c>intif_homunculus_requestsave</c> (intif.cpp:1813).
    /// Snapshots via <see cref="Map.Server.Homunculus.IHomunculusService.SerializeSnapshot"/>
    /// when available, falling back to the legacy <paramref name="data"/>
    /// byte[] payload otherwise (char-side decoder owns it either way).
    /// </summary>
    public int HomunculusSave(byte[] data)
    {
        if (_homunIpc == null) return 0;
        // Legacy byte[] carries the homun_id at offset 0..3 in rAthena.
        // We only need it to look up the live snapshot.
        var homunId = (data != null && data.Length >= 4)
            ? BitConverter.ToInt32(data, 0)
            : 0;
        var snapshot = _homunService?.SerializeSnapshot(homunId);
        if (snapshot == null)
        {
            // Data-pending fallback: dispatch the raw payload so the char
            // side can still write the legacy bytes. Matches rAthena
            // intif_homunculus_requestsave behavior when sd->hd is null.
            snapshot = new Core.Server.IPC.HomunculusData
            {
                HomunculusId = homunId,
                Payload = Google.Protobuf.ByteString.CopyFrom(data ?? Array.Empty<byte>()),
            };
        }
        _ = _homunIpc.HomunculusSaveAsync(accountId: 0, homunculus: snapshot);
        return 1;
    }

    /// <summary>
    /// T7.3 — rAthena <c>intif_homunculus_requestdelete</c>
    /// (intif.cpp:1832).
    /// </summary>
    public int HomunculusDelete(int homunId)
    {
        if (_homunIpc == null) return 0;
        _ = _homunIpc.HomunculusDeleteAsync(homunId);
        return 1;
    }

    /// <summary>
    /// T7.3 — rAthena <c>intif_mercenary_create</c> (intif.cpp:2256).
    /// </summary>
    public int MercenaryCreate(byte[] data)
    {
        if (_mercIpc == null) return 0;
        _ = _mercIpc.MercenaryCreateAsync(
            new Core.Server.IPC.MercenaryData
            {
                Payload = Google.Protobuf.ByteString.CopyFrom(data ?? Array.Empty<byte>()),
            });
        return 1;
    }

    /// <summary>
    /// T7.3 — rAthena <c>intif_mercenary_request</c> (intif.cpp:2274).
    /// </summary>
    public int MercenaryRequest(int accountId, int mercId)
    {
        if (_mercIpc == null) return 0;
        _ = _mercIpc.MercenaryLoadAsync(mercenaryId: mercId, characterId: 0);
        return 1;
    }

    /// <summary>
    /// T7.3 — rAthena <c>intif_mercenary_save</c> (intif.cpp:2294).
    /// </summary>
    public int MercenarySave(byte[] data)
    {
        if (_mercIpc == null) return 0;
        var mercId = (data != null && data.Length >= 4)
            ? BitConverter.ToInt32(data, 0)
            : 0;
        var snapshot = _mercService?.SerializeSnapshot(mercId)
            ?? new Core.Server.IPC.MercenaryData
            {
                MercenaryId = mercId,
                Payload = Google.Protobuf.ByteString.CopyFrom(data ?? Array.Empty<byte>()),
            };
        _ = _mercIpc.MercenarySaveAsync(snapshot);
        return 1;
    }

    /// <summary>
    /// T7.3 — rAthena <c>intif_mercenary_delete</c> (intif.cpp:2313).
    /// </summary>
    public int MercenaryDelete(int mercId)
    {
        if (_mercIpc == null) return 0;
        _ = _mercIpc.MercenaryDeleteAsync(mercId);
        return 1;
    }

    public int ClanMessage(int clanId, int charId, string text) => 0;

    /// <summary>
    /// T7.4 — rAthena <c>intif_request_account_storage</c> (intif.cpp:2438).
    /// Char-side returns the opaque byte payload (account_storage_payload
    /// table); map side decodes via StorageCodec.
    /// </summary>
    public int RequestAccountStorage(int accountId)
    {
        if (_storageIpc == null) return 0;
        _ = _storageIpc.AccountStorageLoadAsync(accountId: accountId, characterId: 0);
        return 1;
    }

    /// <summary>
    /// T7.4 — rAthena <c>intif_send_account_storage</c> (intif.cpp:2456).
    /// Fire-and-forget save of the storage byte payload.
    /// </summary>
    public int SaveAccountStorage(int accountId, byte[] data)
    {
        if (_storageIpc == null) return 0;
        _ = _storageIpc.AccountStorageSaveAsync(accountId: accountId, characterId: 0,
            data: data ?? Array.Empty<byte>());
        return 1;
    }

    /// <summary>
    /// T7.4 — rAthena <c>intif_request_guild_storage</c> (intif.cpp:1066).
    /// </summary>
    public int RequestGuildStorage(int charId, int guildId)
    {
        if (_storageIpc == null) return 0;
        _ = _storageIpc.GuildStorageLoadAsync(guildId);
        return 1;
    }

    /// <summary>
    /// T7.4 — rAthena <c>intif_send_guild_storage</c> (intif.cpp:1080).
    /// </summary>
    public int SaveGuildStorage(int charId, int guildId, byte[] data)
    {
        if (_storageIpc == null) return 0;
        _ = _storageIpc.GuildStorageSaveAsync(guildId, data ?? Array.Empty<byte>());
        return 1;
    }

    public int CreateBg(int mapIndex, byte side) => 0;
    public int BgRecord(int bgId, int charId, byte score) => 0;

    /// <summary>
    /// T7.3 — rAthena <c>intif_elemental_create</c> (intif.cpp:2349).
    /// </summary>
    public int ElementalCreate(byte[] data)
    {
        if (_elemIpc == null) return 0;
        _ = _elemIpc.ElementalCreateAsync(
            new Core.Server.IPC.ElementalData
            {
                // ElementalData has no payload field in the proto today;
                // the create path uses class id from the byte[] header
                // when the decoder lands.
                ClassId = (data != null && data.Length >= 4) ? BitConverter.ToInt32(data, 0) : 0,
            });
        return 1;
    }

    /// <summary>
    /// T7.3 — rAthena <c>intif_elemental_request</c> (intif.cpp:2364).
    /// </summary>
    public int ElementalRequest(int accountId, int eleId)
    {
        if (_elemIpc == null) return 0;
        _ = _elemIpc.ElementalLoadAsync(elementalId: eleId, characterId: 0);
        return 1;
    }

    /// <summary>
    /// T7.3 — rAthena <c>intif_elemental_save</c> (intif.cpp:2384).
    /// </summary>
    public int ElementalSave(byte[] data)
    {
        if (_elemIpc == null) return 0;
        var eleId = (data != null && data.Length >= 4)
            ? BitConverter.ToInt32(data, 0)
            : 0;
        var snapshot = _elemService?.SerializeSnapshot(eleId)
            ?? new Core.Server.IPC.ElementalData { ElementalId = eleId };
        _ = _elemIpc.ElementalSaveAsync(snapshot);
        return 1;
    }

    /// <summary>
    /// T7.3 — rAthena <c>intif_elemental_delete</c> (intif.cpp:2403).
    /// </summary>
    public int ElementalDelete(int eleId)
    {
        if (_elemIpc == null) return 0;
        _ = _elemIpc.ElementalDeleteAsync(eleId);
        return 1;
    }

    /// <summary>
    /// T7.8 — rAthena <c>intif_request_mapreg</c> (intif.cpp:3211).
    /// Dispatches the typed mapreg pull through
    /// <see cref="ICharServerIpcServiceMapreg.RequestMapregAsync"/>.
    /// The char-side gRPC handler + persistence land when the script
    /// engine's <c>$var</c> consumer ports (Phase 4 of
    /// <c>map/scripting/README.md</c>); the canonical seam is in
    /// place either way so the rAthena entry point stays 1:1.
    /// </summary>
    public int RequestMapreg()
    {
        if (_mapregIpc == null) return 0;
        _ = _mapregIpc.RequestMapregAsync();
        return 1;
    }

    /// <summary>
    /// T7.8 — rAthena <c>intif_save_mapreg</c> (intif.cpp:3232). Pushes
    /// the opaque mapreg byte payload through
    /// <see cref="ICharServerIpcServiceMapreg.SaveMapregAsync"/>.
    /// </summary>
    public int SaveMapreg(byte[] data)
    {
        if (_mapregIpc == null) return 0;
        _ = _mapregIpc.SaveMapregAsync(data ?? Array.Empty<byte>());
        return 1;
    }

    public bool CheckConnection() => true;
    public void Init() { }
    public void Final() { }
}
