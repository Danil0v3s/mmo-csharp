using Map.Server.Entities;

namespace Map.Server.Services.Intif;

/// <summary>
/// Map → inter-server façade. Canonical entry points for rAthena
/// <c>intif.cpp</c> (3 900 lines, 149 public functions) — the
/// chattiest IPC surface (party / guild / mail / auction / quest /
/// pet / homun / merc / clan / bg / storage / mapreg).
///
/// The C# port routes all of these through the char-server gRPC
/// `inter` namespace already. This service surfaces the rAthena-
/// named `intif_*` operations so a port can find a single named call.
/// Only the public surface — the per-RPC implementation forwards
/// to the existing `*IpcService` wrappers.
/// </summary>
public interface IIntifService
{
    /// <summary>rAthena <c>intif_request_chatname</c>.</summary>
    int RequestChatName(int charId);
    /// <summary>rAthena <c>intif_request_accinfo</c>.</summary>
    int RequestAccInfo(int callerCharId, int targetCharId);
    /// <summary>rAthena <c>intif_broadcast</c>.</summary>
    int Broadcast(string mes, uint colorRgb, byte type);
    /// <summary>rAthena <c>intif_broadcast2</c>.</summary>
    int Broadcast2(string mes, uint colorRgb, ushort fontType, ushort fontSize, ushort fontAlign, ushort fontY);
    /// <summary>rAthena <c>intif_main_message</c>.</summary>
    int MainMessage(PlayerEntity pc, string text);
    /// <summary>rAthena <c>intif_wis_message</c>.</summary>
    int WisMessage(PlayerEntity from, string toName, string text);
    /// <summary>rAthena <c>intif_wis_message_to_gm</c>.</summary>
    int WisMessageToGm(string from, byte minGmLevel, string text);
    /// <summary>rAthena <c>intif_saveregistry</c>.</summary>
    int SaveRegistry(PlayerEntity pc);
    /// <summary>rAthena <c>intif_request_registry</c>.</summary>
    int RequestRegistry(PlayerEntity pc, byte flag);

    // -- party --
    int CreateParty(PlayerEntity pc, string name, byte item, byte itemDiv);
    int RequestPartyInfo(int partyId, int charId);
    int AddPartyMember(int partyId, PlayerEntity pc);
    int ChangePartyLeader(int partyId, int accountId, int charId);
    int PartyChangeOption(int partyId, int accountId, int exp, int item, int flag);
    /// <param name="reason">Withdraw reason byte broadcast to the party — 0 = left voluntarily
    /// (PARTY_MEMBER_WITHDRAW), 1 = expelled/kicked (PARTY_MEMBER_EXPEL). rAthena party.cpp.</param>
    int LeaveParty(int partyId, int accountId, int charId, byte reason = 0);
    int PartyChangemap(PlayerEntity pc, bool online);
    int BreakParty(int partyId);
    int PartyMessage(int partyId, int accountId, string text);

    // -- guild --
    bool GuildCreate(string name, PlayerEntity master);
    int GuildRequestInfo(int guildId);
    int GuildAddMember(int guildId, PlayerEntity pc);
    int GuildLeave(int guildId, int accountId, int charId, byte flag, string mes);
    int GuildExpulsion(int guildId, int accountId, int charId, string mes);
    int GuildBreak(int guildId);
    int GuildMessage(int guildId, int accountId, string mes);
    int GuildEmblem(int guildId, byte[] emblem);
    int GuildSavePosition(int guildId, byte idx, int mode, int exp_mode, string name);
    int GuildSetSkill(int guildId, ushort skillId, ushort skillLevel);
    int GuildAllianceAck(int guildId, int allyId, int accountId, int charId, int flag, string mes);
    int GuildAddCastle(int castleId, int guildId);

    // -- mail --
    int MailRequestInbox(int charId, byte flag);
    int MailRead(int mailId);
    int MailGetAttach(int charId, int mailId, byte flag);
    int MailDelete(int charId, int mailId);
    int MailSend(int senderCharId, string toName, string title, string body, int zeny);
    int MailReturn(int charId, int mailId);

    // -- auction --
    int AuctionRequestList(int charId, byte type, int price, string search, byte page);
    int AuctionRegister(int charId, byte type, int sellerCharId, string sellerName, int now, int hours, int priceStart, int priceBuyNow, int itemId, byte refine, byte attribute, int identify, int amount);
    int AuctionCancel(int charId, uint auctionId);
    int AuctionClose(int charId, uint auctionId);
    int AuctionBid(int charId, uint auctionId, int bid, string bidder);

    // -- quest --
    int QuestSave(PlayerEntity pc);
    int QuestRequest(int charId);

    /// <summary>Awaitable load-on-enter: fetch the char-side quest log, hydrate it onto
    /// <paramref name="pc"/>, then push the quest window (<c>quest_pc_login</c>). Used by the
    /// LoadEndAck spawn cascade so quests exist on the entity and render at session start.</summary>
    Task QuestRequestAsync(PlayerEntity pc, CancellationToken ct = default);

    // -- achievement --
    int AchievementSave(PlayerEntity pc);
    int AchievementRequest(int charId);

    /// <summary>GP-ACHIEVE — awaitable load-on-enter: fetch the char-side achievement log, hydrate it
    /// onto <paramref name="pc"/>, then push the achievement window (<c>clif_achievement_list_all</c>).
    /// Used by the LoadEndAck spawn cascade so achievements exist on the entity and render at session
    /// start (mirrors rAthena's intif_request_achievementlist → mapif_load_achievements tail of
    /// pc_authok).</summary>
    Task AchievementRequestAsync(PlayerEntity pc, CancellationToken ct = default);

    // FEATURE-02 — awaitable save variants for the game-loop save fan-out: the final-save
    // (logout) path awaits these so the char-server row lands before the session is torn down.
    // The int wrappers above stay the fire-and-forget autosave path (they delegate to these).
    Task QuestSaveAsync(PlayerEntity pc, CancellationToken ct = default);
    Task AchievementSaveAsync(PlayerEntity pc, CancellationToken ct = default);

    // -- pet / homun / merc --
    int PetCreate(PlayerEntity master, int classId, int nameId, byte rename, int eggItemId, byte intimate, byte hungry, char gender, string petName);

    /// <summary>Awaitable pet create: the char server inserts the pet row and returns its new pet_id
    /// (0 on failure / no IPC). The caller binds that id into the granted egg's card slots.</summary>
    Task<int> PetCreateAsync(PlayerEntity master, int classId, int eggItemId, byte intimate, byte hungry, string petName, CancellationToken ct = default);

    /// <summary>Awaitable pet load: fetch the saved <see cref="Core.Server.IPC.PetData"/> for a bound
    /// pet_id (null on failure / no IPC), so a hatched egg restores the pet's intimacy/hunger/name.</summary>
    Task<Core.Server.IPC.PetData?> PetLoadAsync(int petId, int accountId, int charId, CancellationToken ct = default);
    int RequestPetInfo(int petId, int accountId, byte flag);
    int SavePet(int petId);
    Task SavePetAsync(int petId, CancellationToken ct = default);
    int DeletePet(int petId);
    int HomunculusCreate(int accountId, byte[] data);
    int HomunculusRequest(int accountId, int homunId);
    int HomunculusSave(byte[] data);
    int HomunculusDelete(int homunId);
    int MercenaryCreate(byte[] data);
    int MercenaryRequest(int accountId, int mercId);
    int MercenarySave(byte[] data);
    int MercenaryDelete(int mercId);

    // -- clan --
    int ClanMessage(int clanId, int charId, string text);

    // -- storage --
    int RequestAccountStorage(int accountId);
    int SaveAccountStorage(int accountId, byte[] data);
    int RequestGuildStorage(int charId, int guildId);
    int SaveGuildStorage(int charId, int guildId, byte[] data);

    // -- bg --
    int CreateBg(int mapIndex, byte side);
    int BgRecord(int bgId, int charId, byte score);

    // -- elemental --
    int ElementalCreate(byte[] data);
    int ElementalRequest(int accountId, int eleId);
    int ElementalSave(byte[] data);
    int ElementalDelete(int eleId);

    // -- mapreg --
    int RequestMapreg();
    int SaveMapreg(byte[] data);

    /// <summary>rAthena <c>intif_check_connection</c>.</summary>
    bool CheckConnection();

    void Init();
    void Final();
}
