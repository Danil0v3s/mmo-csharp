using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Context;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    // Account & Login
    public DbSet<LoginEntity> Logins { get; set; }
    public DbSet<AccRegNumEntity> AccountRegistersNum { get; set; }
    public DbSet<AccRegStrEntity> AccountRegistersStr { get; set; }
    public DbSet<GlobalAccRegNumEntity> GlobalAccountRegistersNum { get; set; }
    public DbSet<GlobalAccRegStrEntity> GlobalAccountRegistersStr { get; set; }
    
    // Character
    public DbSet<CharEntity> Characters { get; set; }
    public DbSet<CharRegNumEntity> CharacterRegistersNum { get; set; }
    public DbSet<CharRegStrEntity> CharacterRegistersStr { get; set; }
    public DbSet<CharLogEntity> CharacterLogs { get; set; }
    
    // Inventory
    public DbSet<InventoryEntity> Inventories { get; set; }
    public DbSet<CartInventoryEntity> CartInventories { get; set; }
    public DbSet<StorageEntity> Storages { get; set; }
    public DbSet<GuildStorageEntity> GuildStorages { get; set; }
    public DbSet<GuildStorageLogEntity> GuildStorageLogs { get; set; }
    
    // Guild
    public DbSet<GuildEntity> Guilds { get; set; }
    public DbSet<GuildAllianceEntity> GuildAlliances { get; set; }
    public DbSet<GuildCastleEntity> GuildCastles { get; set; }
    public DbSet<GuildExpulsionEntity> GuildExpulsions { get; set; }
    public DbSet<GuildMemberEntity> GuildMembers { get; set; }
    public DbSet<GuildPositionEntity> GuildPositions { get; set; }
    public DbSet<GuildSkillEntity> GuildSkills { get; set; }
    
    // Clan
    public DbSet<ClanEntity> Clans { get; set; }
    public DbSet<ClanAllianceEntity> ClanAlliances { get; set; }
    
    // Party
    public DbSet<PartyEntity> Parties { get; set; }
    public DbSet<PartyBookingEntity> PartyBookings { get; set; }
    
    // Social
    public DbSet<FriendEntity> Friends { get; set; }
    public DbSet<MailEntity> Mails { get; set; }
    public DbSet<MailAttachmentEntity> MailAttachments { get; set; }
    
    // Pets & Companions
    public DbSet<PetEntity> Pets { get; set; }
    public DbSet<HomunculusEntity> Homunculi { get; set; }
    public DbSet<ElementalEntity> Elementals { get; set; }
    public DbSet<MercenaryEntity> Mercenaries { get; set; }
    public DbSet<MercenaryOwnerEntity> MercenaryOwners { get; set; }
    
    // Skills
    public DbSet<SkillEntity> Skills { get; set; }
    public DbSet<SkillHomunculusEntity> SkillHomunculi { get; set; }
    public DbSet<SkillCooldownEntity> SkillCooldowns { get; set; }
    public DbSet<SkillCooldownHomunculusEntity> SkillCooldownHomunculi { get; set; }
    public DbSet<SkillCooldownMercenaryEntity> SkillCooldownMercenaries { get; set; }
    
    // Quests & Achievements
    public DbSet<QuestEntity> Quests { get; set; }
    public DbSet<AchievementEntity> Achievements { get; set; }
    
    // Status & Effects
    public DbSet<BonusScriptEntity> BonusScripts { get; set; }
    public DbSet<ScDataEntity> StatusChanges { get; set; }
    public DbSet<HotkeyEntity> Hotkeys { get; set; }
    public DbSet<MemoEntity> Memos { get; set; }
    
    // Commerce
    public DbSet<AuctionEntity> Auctions { get; set; }
    public DbSet<VendingEntity> Vendings { get; set; }
    public DbSet<VendingItemEntity> VendingItems { get; set; }
    public DbSet<BuyingStoreEntity> BuyingStores { get; set; }
    public DbSet<BuyingStoreItemEntity> BuyingStoreItems { get; set; }
    public DbSet<MarketEntity> Markets { get; set; }
    public DbSet<BarterEntity> Barters { get; set; }
    public DbSet<SaleEntity> Sales { get; set; }
    
    // System
    public DbSet<MapRegEntity> MapRegisters { get; set; }
    public DbSet<InterLogEntity> InterLogs { get; set; }
    public DbSet<IpBanListEntity> IpBanLists { get; set; }
    public DbSet<DbRouletteEntity> RouletteItems { get; set; }
    
    // Logs
    public DbSet<AtCommandLogEntity> AtCommandLogs { get; set; }
    public DbSet<BranchLogEntity> BranchLogs { get; set; }
    public DbSet<CashLogEntity> CashLogs { get; set; }
    public DbSet<ChatLogEntity> ChatLogs { get; set; }
    public DbSet<FeedingLogEntity> FeedingLogs { get; set; }
    public DbSet<LoginLogEntity> LoginLogs { get; set; }
    public DbSet<MvpLogEntity> MvpLogs { get; set; }
    public DbSet<NpcLogEntity> NpcLogs { get; set; }
    public DbSet<PickLogEntity> PickLogs { get; set; }
    public DbSet<ZenyLogEntity> ZenyLogs { get; set; }
    
    // Web
    public DbSet<GuildEmblemEntity> GuildEmblems { get; set; }
    public DbSet<UserConfigEntity> UserConfigs { get; set; }
    public DbSet<CharConfigEntity> CharConfigs { get; set; }
    public DbSet<MerchantConfigEntity> MerchantConfigs { get; set; }
    
    public DbSet<ItemEntity> ItemDb => Set<ItemEntity>();
    
    public DbSet<MobEntity> MobDb => Set<MobEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameDbContext).Assembly);
    }
}

