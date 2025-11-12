using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class ItemEntityConfiguration : IEntityTypeConfiguration<ItemEntity>
{
    public void Configure(EntityTypeBuilder<ItemEntity> builder)
    {
        builder.ToTable("item_db");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.NameAegis).HasColumnName("name_aegis").HasMaxLength(50).IsRequired();
        builder.Property(e => e.NameEnglish).HasColumnName("name_english").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Type).HasColumnName("type").HasMaxLength(20);
        builder.Property(e => e.Subtype).HasColumnName("subtype").HasMaxLength(20);
        builder.Property(e => e.PriceBuy).HasColumnName("price_buy");
        builder.Property(e => e.PriceSell).HasColumnName("price_sell");
        builder.Property(e => e.Weight).HasColumnName("weight");
        builder.Property(e => e.Attack).HasColumnName("attack");
        builder.Property(e => e.MagicAttack).HasColumnName("magic_attack");
        builder.Property(e => e.Defense).HasColumnName("defense");
        builder.Property(e => e.Range).HasColumnName("range");
        builder.Property(e => e.Slots).HasColumnName("slots");
        
        // Job restrictions
        builder.Property(e => e.JobAll).HasColumnName("job_all");
        builder.Property(e => e.JobAcolyte).HasColumnName("job_acolyte");
        builder.Property(e => e.JobAlchemist).HasColumnName("job_alchemist");
        builder.Property(e => e.JobArcher).HasColumnName("job_archer");
        builder.Property(e => e.JobAssassin).HasColumnName("job_assassin");
        builder.Property(e => e.JobBarddancer).HasColumnName("job_barddancer");
        builder.Property(e => e.JobBlacksmith).HasColumnName("job_blacksmith");
        builder.Property(e => e.JobCrusader).HasColumnName("job_crusader");
        builder.Property(e => e.JobGunslinger).HasColumnName("job_gunslinger");
        builder.Property(e => e.JobHunter).HasColumnName("job_hunter");
        builder.Property(e => e.JobKagerouoboro).HasColumnName("job_kagerouoboro");
        builder.Property(e => e.JobKnight).HasColumnName("job_knight");
        builder.Property(e => e.JobMage).HasColumnName("job_mage");
        builder.Property(e => e.JobMerchant).HasColumnName("job_merchant");
        builder.Property(e => e.JobMonk).HasColumnName("job_monk");
        builder.Property(e => e.JobNinja).HasColumnName("job_ninja");
        builder.Property(e => e.JobNovice).HasColumnName("job_novice");
        builder.Property(e => e.JobPriest).HasColumnName("job_priest");
        builder.Property(e => e.JobRebellion).HasColumnName("job_rebellion");
        builder.Property(e => e.JobRogue).HasColumnName("job_rogue");
        builder.Property(e => e.JobSage).HasColumnName("job_sage");
        builder.Property(e => e.JobSoullinker).HasColumnName("job_soullinker");
        builder.Property(e => e.JobSpiritHandler).HasColumnName("job_spirit_handler");
        builder.Property(e => e.JobStargladiator).HasColumnName("job_stargladiator");
        builder.Property(e => e.JobSummoner).HasColumnName("job_summoner");
        builder.Property(e => e.JobSupernovice).HasColumnName("job_supernovice");
        builder.Property(e => e.JobSwordman).HasColumnName("job_swordman");
        builder.Property(e => e.JobTaekwon).HasColumnName("job_taekwon");
        builder.Property(e => e.JobThief).HasColumnName("job_thief");
        builder.Property(e => e.JobWizard).HasColumnName("job_wizard");
        
        // Class restrictions
        builder.Property(e => e.ClassAll).HasColumnName("class_all");
        builder.Property(e => e.ClassNormal).HasColumnName("class_normal");
        builder.Property(e => e.ClassUpper).HasColumnName("class_upper");
        builder.Property(e => e.ClassBaby).HasColumnName("class_baby");
        builder.Property(e => e.ClassThird).HasColumnName("class_third");
        builder.Property(e => e.ClassThirdUpper).HasColumnName("class_third_upper");
        builder.Property(e => e.ClassThirdBaby).HasColumnName("class_third_baby");
        builder.Property(e => e.ClassFourth).HasColumnName("class_fourth");
        
        builder.Property(e => e.Gender).HasColumnName("gender").HasMaxLength(10);
        
        // Equipment locations
        builder.Property(e => e.LocationHeadTop).HasColumnName("location_head_top");
        builder.Property(e => e.LocationHeadMid).HasColumnName("location_head_mid");
        builder.Property(e => e.LocationHeadLow).HasColumnName("location_head_low");
        builder.Property(e => e.LocationArmor).HasColumnName("location_armor");
        builder.Property(e => e.LocationRightHand).HasColumnName("location_right_hand");
        builder.Property(e => e.LocationLeftHand).HasColumnName("location_left_hand");
        builder.Property(e => e.LocationGarment).HasColumnName("location_garment");
        builder.Property(e => e.LocationShoes).HasColumnName("location_shoes");
        builder.Property(e => e.LocationRightAccessory).HasColumnName("location_right_accessory");
        builder.Property(e => e.LocationLeftAccessory).HasColumnName("location_left_accessory");
        builder.Property(e => e.LocationCostumeHeadTop).HasColumnName("location_costume_head_top");
        builder.Property(e => e.LocationCostumeHeadMid).HasColumnName("location_costume_head_mid");
        builder.Property(e => e.LocationCostumeHeadLow).HasColumnName("location_costume_head_low");
        builder.Property(e => e.LocationCostumeGarment).HasColumnName("location_costume_garment");
        builder.Property(e => e.LocationAmmo).HasColumnName("location_ammo");
        builder.Property(e => e.LocationShadowArmor).HasColumnName("location_shadow_armor");
        builder.Property(e => e.LocationShadowWeapon).HasColumnName("location_shadow_weapon");
        builder.Property(e => e.LocationShadowShield).HasColumnName("location_shadow_shield");
        builder.Property(e => e.LocationShadowShoes).HasColumnName("location_shadow_shoes");
        builder.Property(e => e.LocationShadowRightAccessory).HasColumnName("location_shadow_right_accessory");
        builder.Property(e => e.LocationShadowLeftAccessory).HasColumnName("location_shadow_left_accessory");
        
        // Equipment properties
        builder.Property(e => e.WeaponLevel).HasColumnName("weapon_level");
        builder.Property(e => e.ArmorLevel).HasColumnName("armor_level");
        builder.Property(e => e.EquipLevelMin).HasColumnName("equip_level_min");
        builder.Property(e => e.EquipLevelMax).HasColumnName("equip_level_max");
        builder.Property(e => e.Refineable).HasColumnName("refineable");
        builder.Property(e => e.Gradable).HasColumnName("gradable");
        builder.Property(e => e.View).HasColumnName("view");
        builder.Property(e => e.AliasName).HasColumnName("alias_name").HasMaxLength(50);
        
        // Flags
        builder.Property(e => e.FlagBuyingstore).HasColumnName("flag_buyingstore");
        builder.Property(e => e.FlagDeadbranch).HasColumnName("flag_deadbranch");
        builder.Property(e => e.FlagContainer).HasColumnName("flag_container");
        builder.Property(e => e.FlagUniqueid).HasColumnName("flag_uniqueid");
        builder.Property(e => e.FlagBindonequip).HasColumnName("flag_bindonequip");
        builder.Property(e => e.FlagDropannounce).HasColumnName("flag_dropannounce");
        builder.Property(e => e.FlagNoconsume).HasColumnName("flag_noconsume");
        builder.Property(e => e.FlagDropeffect).HasColumnName("flag_dropeffect").HasMaxLength(20);
        
        // Delay
        builder.Property(e => e.DelayDuration).HasColumnName("delay_duration");
        builder.Property(e => e.DelayStatus).HasColumnName("delay_status").HasMaxLength(30);
        
        // Stack
        builder.Property(e => e.StackAmount).HasColumnName("stack_amount");
        builder.Property(e => e.StackInventory).HasColumnName("stack_inventory");
        builder.Property(e => e.StackCart).HasColumnName("stack_cart");
        builder.Property(e => e.StackStorage).HasColumnName("stack_storage");
        builder.Property(e => e.StackGuildstorage).HasColumnName("stack_guildstorage");
        
        // Usage restrictions
        builder.Property(e => e.NouseOverride).HasColumnName("nouse_override");
        builder.Property(e => e.NouseSitting).HasColumnName("nouse_sitting");
        
        // Trade restrictions
        builder.Property(e => e.TradeOverride).HasColumnName("trade_override");
        builder.Property(e => e.TradeNodrop).HasColumnName("trade_nodrop");
        builder.Property(e => e.TradeNotrade).HasColumnName("trade_notrade");
        builder.Property(e => e.TradeTradepartner).HasColumnName("trade_tradepartner");
        builder.Property(e => e.TradeNosell).HasColumnName("trade_nosell");
        builder.Property(e => e.TradeNocart).HasColumnName("trade_nocart");
        builder.Property(e => e.TradeNostorage).HasColumnName("trade_nostorage");
        builder.Property(e => e.TradeNoguildstorage).HasColumnName("trade_noguildstorage");
        builder.Property(e => e.TradeNomail).HasColumnName("trade_nomail");
        builder.Property(e => e.TradeNoauction).HasColumnName("trade_noauction");
        
        // Scripts (TEXT fields)
        builder.Property(e => e.Script).HasColumnName("script").HasColumnType("text");
        builder.Property(e => e.EquipScript).HasColumnName("equip_script").HasColumnType("text");
        builder.Property(e => e.UnequipScript).HasColumnName("unequip_script").HasColumnType("text");
        
        // Indexes
        builder.HasIndex(e => e.NameAegis).IsUnique().HasDatabaseName("UniqueAegisName");
    }
}