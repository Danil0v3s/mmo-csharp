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
    public DbSet<AccountStoragePayloadEntity> AccountStoragePayloads { get; set; }
    public DbSet<GuildStoragePayloadEntity> GuildStoragePayloads { get; set; }
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

    /// <summary>
    /// rAthena <c>mob_skill_db</c> catalog — one row per (mob, skill)
    /// AI rule. Seeded from rAthena's pre-generated
    /// <c>sql-files/mob_skill_db_re.sql</c> (11,634 rows).
    /// </summary>
    public DbSet<MobSkillDbEntity> MobSkillDb => Set<MobSkillDbEntity>();

    /// <summary>
    /// Static skill catalog (rAthena <c>skill_db</c> table — equivalent
    /// of <c>db/re/skill_db.yml</c> under <c>use_sql_db: yes</c>). Empty
    /// today; consumed by <c>Map.Server.Skills.SkillDb</c> when seeded.
    /// </summary>
    public DbSet<SkillDbEntity> SkillDb => Set<SkillDbEntity>();

    // ---- Static catalogs seeded by Tools.RathenaImporter from rAthena YAML.
    public DbSet<AbraDbEntity> AbraDb => Set<AbraDbEntity>();
    public DbSet<MagicMushroomDbEntity> MagicMushroomDb => Set<MagicMushroomDbEntity>();
    public DbSet<SpellbookDbEntity> SpellbookDb => Set<SpellbookDbEntity>();
    public DbSet<QuestDbEntity> QuestDb => Set<QuestDbEntity>();
    public DbSet<PetDbEntity> PetDb => Set<PetDbEntity>();
    public DbSet<AchievementDbEntity> AchievementDb => Set<AchievementDbEntity>();
    public DbSet<HomunculusDbEntity> HomunculusDb => Set<HomunculusDbEntity>();
    public DbSet<MercenaryDbEntity> MercenaryDb => Set<MercenaryDbEntity>();
    public DbSet<InstanceDbEntity> InstanceDb => Set<InstanceDbEntity>();

    // ---- AT-F: nested-array child tables previously baked inline ----
    // (battleground_db is already in the JSON-payload catalog table —
    //  see CatalogEntities.BattlegroundDbEntity; consume via DB-8.)
    public DbSet<MercenarySkillDbEntity> MercenarySkillDb => Set<MercenarySkillDbEntity>();
    public DbSet<HomunculusSkillTreeDbEntity> HomunculusSkillTreeDb => Set<HomunculusSkillTreeDbEntity>();

    // ---- AT-G: typed catalogs that DB-1..6 skipped entirely ----
    public DbSet<StylistDbEntity> StylistDb => Set<StylistDbEntity>();
    public DbSet<AchievementLevelDbEntity> AchievementLevelDb => Set<AchievementLevelDbEntity>();
    public DbSet<JobAspdDbEntity> JobAspdDb => Set<JobAspdDbEntity>();
    public DbSet<ConstDbEntity> ConstDb => Set<ConstDbEntity>();

    // ---- Second wave of static catalogs (flat-shape) ----
    public DbSet<CastleDbEntity> CastleDb => Set<CastleDbEntity>();
    public DbSet<StatPointEntity> StatPointDb => Set<StatPointEntity>();
    public DbSet<ExpHomunEntity> ExpHomunDb => Set<ExpHomunEntity>();
    public DbSet<ExpGuildEntity> ExpGuildDb => Set<ExpGuildEntity>();
    public DbSet<SizeFixEntity> SizeFixDb => Set<SizeFixEntity>();
    public DbSet<ReputationEntity> ReputationDb => Set<ReputationEntity>();
    public DbSet<CreateArrowDbEntity> CreateArrowDb => Set<CreateArrowDbEntity>();
    public DbSet<ItemRandomOptDbEntity> ItemRandomOptDb => Set<ItemRandomOptDbEntity>();
    public DbSet<CashShopDbEntity> CashShopDb => Set<CashShopDbEntity>();
    public DbSet<CaptchaDbEntity> CaptchaDb => Set<CaptchaDbEntity>();

    // ---- Payload-shape catalogs (key + JSON payload) ----
    public DbSet<ElementalDbEntity> ElementalDb => Set<ElementalDbEntity>();
    public DbSet<BattlegroundDbEntity> BattlegroundDb => Set<BattlegroundDbEntity>();
    // DB-8c: SkillTreeDb / GuildSkillTreeDb moved to typed entities below.
    // DB-8b: MobSummonDb moved to typed entity below.
    public DbSet<ItemRandomOptGroupEntity> ItemRandomOptGroupDb => Set<ItemRandomOptGroupEntity>();
    // DB-8a: AttrFixDb / LevelPenaltyDb moved to typed entities below.
    // DB-8d: JobStatsDb / JobExpDb / JobBasePointsDb moved to typed entities below.
    public DbSet<StatusYmlEntity> StatusYmlDb => Set<StatusYmlEntity>();
    // DB-8b: ItemCombosDb / ItemPackagesDb / ItemGroupDb moved to typed entities below.
    public DbSet<ItemEnchantEntity> ItemEnchantDb => Set<ItemEnchantEntity>();
    public DbSet<ItemReformEntity> ItemReformDb => Set<ItemReformEntity>();
    public DbSet<LaphineSynthesisEntity> LaphineSynthesisDb => Set<LaphineSynthesisEntity>();
    public DbSet<LaphineUpgradeEntity> LaphineUpgradeDb => Set<LaphineUpgradeEntity>();
    public DbSet<RefineEntity> RefineDb => Set<RefineEntity>();
    public DbSet<EnchantGradeEntity> EnchantGradeDb => Set<EnchantGradeEntity>();
    public DbSet<MapDropsEntity> MapDropsDb => Set<MapDropsEntity>();
    public DbSet<MobItemRatioEntity> MobItemRatioDb => Set<MobItemRatioEntity>();
    // DB-8b: ItemCashDb / AttendanceDb moved to typed entities below.
    // DB-8a: ReputationGroupDb moved to typed entity below.

    // ---- DB-8a: re-normalized typed catalogs (was PayloadJson blobs) ----
    public DbSet<AttrFixDbEntity> AttrFixDb => Set<AttrFixDbEntity>();
    public DbSet<LevelPenaltyDbEntity> LevelPenaltyDb => Set<LevelPenaltyDbEntity>();
    public DbSet<LevelPenaltyDifferenceDbEntity> LevelPenaltyDifferenceDb => Set<LevelPenaltyDifferenceDbEntity>();
    public DbSet<ReputationGroupDbEntity> ReputationGroupDb => Set<ReputationGroupDbEntity>();
    public DbSet<ReputationGroupMemberDbEntity> ReputationGroupMemberDb => Set<ReputationGroupMemberDbEntity>();

    // ---- DB-8b: re-normalized typed catalogs (was PayloadJson blobs) ----
    public DbSet<MobSummonDbEntity> MobSummonDb => Set<MobSummonDbEntity>();
    public DbSet<MobSummonEntryDbEntity> MobSummonEntryDb => Set<MobSummonEntryDbEntity>();
    public DbSet<AttendanceCatalogDbEntity> AttendanceCatalogDb => Set<AttendanceCatalogDbEntity>();
    public DbSet<AttendanceCatalogRewardDbEntity> AttendanceCatalogRewardDb => Set<AttendanceCatalogRewardDbEntity>();
    public DbSet<ItemCashDbEntity> ItemCashDb => Set<ItemCashDbEntity>();
    public DbSet<ItemCashEntryDbEntity> ItemCashEntryDb => Set<ItemCashEntryDbEntity>();
    public DbSet<ItemGroupCatalogDbEntity> ItemGroupCatalogDb => Set<ItemGroupCatalogDbEntity>();
    public DbSet<ItemGroupCatalogEntryDbEntity> ItemGroupCatalogEntryDb => Set<ItemGroupCatalogEntryDbEntity>();
    public DbSet<ItemPackageDbEntity> ItemPackageDb => Set<ItemPackageDbEntity>();
    public DbSet<ItemPackageEntryDbEntity> ItemPackageEntryDb => Set<ItemPackageEntryDbEntity>();
    public DbSet<ItemComboDbEntity> ItemComboDb => Set<ItemComboDbEntity>();
    public DbSet<ItemComboMemberDbEntity> ItemComboMemberDb => Set<ItemComboMemberDbEntity>();

    // ---- DB-8c: skill tree re-normalized typed catalogs ----
    public DbSet<SkillTreeDbEntity> SkillTreeDb => Set<SkillTreeDbEntity>();
    public DbSet<SkillTreeInheritDbEntity> SkillTreeInheritDb => Set<SkillTreeInheritDbEntity>();
    public DbSet<SkillTreeEntryDbEntity> SkillTreeEntryDb => Set<SkillTreeEntryDbEntity>();
    public DbSet<SkillTreeRequirementDbEntity> SkillTreeRequirementDb => Set<SkillTreeRequirementDbEntity>();
    public DbSet<GuildSkillTreeDbEntity> GuildSkillTreeDb => Set<GuildSkillTreeDbEntity>();
    public DbSet<GuildSkillTreeRequirementDbEntity> GuildSkillTreeRequirementDb => Set<GuildSkillTreeRequirementDbEntity>();

    // ---- DB-8d: job table re-normalized typed catalogs ----
    public DbSet<JobInfoDbEntity> JobInfoDb => Set<JobInfoDbEntity>();
    public DbSet<JobBonusStatsDbEntity> JobBonusStatsDb => Set<JobBonusStatsDbEntity>();
    public DbSet<JobExpDbEntity> JobExpDb => Set<JobExpDbEntity>();
    public DbSet<JobMaxLevelDbEntity> JobMaxLevelDb => Set<JobMaxLevelDbEntity>();
    public DbSet<JobBasePointsDbEntity> JobBasePointsDb => Set<JobBasePointsDbEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameDbContext).Assembly);
    }
}
