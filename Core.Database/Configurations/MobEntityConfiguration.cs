using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class MobEntityConfiguration : IEntityTypeConfiguration<MobEntity>
{
    public void Configure(EntityTypeBuilder<MobEntity> builder)
    {
        builder.ToTable("mob_db");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.NameAegis).HasColumnName("name_aegis").HasMaxLength(24).IsRequired();
        builder.Property(e => e.NameEnglish).HasColumnName("name_english").HasColumnType("text").IsRequired();
        builder.Property(e => e.NameJapanese).HasColumnName("name_japanese").HasColumnType("text");
        builder.Property(e => e.Level).HasColumnName("level");
        builder.Property(e => e.Hp).HasColumnName("hp");
        builder.Property(e => e.Sp).HasColumnName("sp");
        builder.Property(e => e.BaseExp).HasColumnName("base_exp");
        builder.Property(e => e.JobExp).HasColumnName("job_exp");
        builder.Property(e => e.MvpExp).HasColumnName("mvp_exp");
        builder.Property(e => e.Attack).HasColumnName("attack");
        builder.Property(e => e.Attack2).HasColumnName("attack2");
        builder.Property(e => e.Defense).HasColumnName("defense");
        builder.Property(e => e.MagicDefense).HasColumnName("magic_defense");
        builder.Property(e => e.Resistance).HasColumnName("resistance");
        builder.Property(e => e.MagicResistance).HasColumnName("magic_resistance");
        
        // Stats
        builder.Property(e => e.Str).HasColumnName("str");
        builder.Property(e => e.Agi).HasColumnName("agi");
        builder.Property(e => e.Vit).HasColumnName("vit");
        builder.Property(e => e.Int).HasColumnName("int");
        builder.Property(e => e.Dex).HasColumnName("dex");
        builder.Property(e => e.Luk).HasColumnName("luk");
        
        // Ranges
        builder.Property(e => e.AttackRange).HasColumnName("attack_range");
        builder.Property(e => e.SkillRange).HasColumnName("skill_range");
        builder.Property(e => e.ChaseRange).HasColumnName("chase_range");
        
        // Type info
        builder.Property(e => e.Size).HasColumnName("size").HasMaxLength(24);
        builder.Property(e => e.Race).HasColumnName("race").HasMaxLength(24);
        
        // Race groups
        builder.Property(e => e.RacegroupGoblin).HasColumnName("racegroup_goblin");
        builder.Property(e => e.RacegroupKobold).HasColumnName("racegroup_kobold");
        builder.Property(e => e.RacegroupOrc).HasColumnName("racegroup_orc");
        builder.Property(e => e.RacegroupGolem).HasColumnName("racegroup_golem");
        builder.Property(e => e.RacegroupGuardian).HasColumnName("racegroup_guardian");
        builder.Property(e => e.RacegroupNinja).HasColumnName("racegroup_ninja");
        builder.Property(e => e.RacegroupGvg).HasColumnName("racegroup_gvg");
        builder.Property(e => e.RacegroupBattlefield).HasColumnName("racegroup_battlefield");
        builder.Property(e => e.RacegroupTreasure).HasColumnName("racegroup_treasure");
        builder.Property(e => e.RacegroupBiolab).HasColumnName("racegroup_biolab");
        builder.Property(e => e.RacegroupManuk).HasColumnName("racegroup_manuk");
        builder.Property(e => e.RacegroupSplendide).HasColumnName("racegroup_splendide");
        builder.Property(e => e.RacegroupScaraba).HasColumnName("racegroup_scaraba");
        builder.Property(e => e.RacegroupOghAtkDef).HasColumnName("racegroup_ogh_atk_def");
        builder.Property(e => e.RacegroupOghHidden).HasColumnName("racegroup_ogh_hidden");
        builder.Property(e => e.RacegroupBio5SwordmanThief).HasColumnName("racegroup_bio5_swordman_thief");
        builder.Property(e => e.RacegroupBio5AcolyteMerchant).HasColumnName("racegroup_bio5_acolyte_merchant");
        builder.Property(e => e.RacegroupBio5MageArcher).HasColumnName("racegroup_bio5_mage_archer");
        builder.Property(e => e.RacegroupBio5Mvp).HasColumnName("racegroup_bio5_mvp");
        builder.Property(e => e.RacegroupClocktower).HasColumnName("racegroup_clocktower");
        builder.Property(e => e.RacegroupThanatos).HasColumnName("racegroup_thanatos");
        builder.Property(e => e.RacegroupFaceworm).HasColumnName("racegroup_faceworm");
        builder.Property(e => e.RacegroupHearthunter).HasColumnName("racegroup_hearthunter");
        builder.Property(e => e.RacegroupRockridge).HasColumnName("racegroup_rockridge");
        builder.Property(e => e.RacegroupWernerLab).HasColumnName("racegroup_werner_lab");
        builder.Property(e => e.RacegroupTempleDemon).HasColumnName("racegroup_temple_demon");
        builder.Property(e => e.RacegroupIllusionVampire).HasColumnName("racegroup_illusion_vampire");
        builder.Property(e => e.RacegroupMalangdo).HasColumnName("racegroup_malangdo");
        builder.Property(e => e.RacegroupEp172alpha).HasColumnName("racegroup_ep172alpha");
        builder.Property(e => e.RacegroupEp172beta).HasColumnName("racegroup_ep172beta");
        builder.Property(e => e.RacegroupEp172bath).HasColumnName("racegroup_ep172bath");
        builder.Property(e => e.RacegroupIllusionTurtle).HasColumnName("racegroup_illusion_turtle");
        builder.Property(e => e.RacegroupRachelSanctuary).HasColumnName("racegroup_rachel_sanctuary");
        builder.Property(e => e.RacegroupIllusionLuanda).HasColumnName("racegroup_illusion_luanda");
        builder.Property(e => e.RacegroupIllusionFrozen).HasColumnName("racegroup_illusion_frozen");
        builder.Property(e => e.RacegroupIllusionMoonlight).HasColumnName("racegroup_illusion_moonlight");
        builder.Property(e => e.RacegroupEp16Def).HasColumnName("racegroup_ep16_def");
        builder.Property(e => e.RacegroupEddaArunafeltz).HasColumnName("racegroup_edda_arunafeltz");
        builder.Property(e => e.RacegroupLasagna).HasColumnName("racegroup_lasagna");
        builder.Property(e => e.RacegroupGlastHeimAbyss).HasColumnName("racegroup_glast_heim_abyss");
        builder.Property(e => e.RacegroupDestroyedValkyrieRealm).HasColumnName("racegroup_destroyed_valkyrie_realm");
        builder.Property(e => e.RacegroupEncroachedGephenia).HasColumnName("racegroup_encroached_gephenia");
        
        // Element
        builder.Property(e => e.Element).HasColumnName("element").HasMaxLength(24);
        builder.Property(e => e.ElementLevel).HasColumnName("element_level");
        
        // Timing
        builder.Property(e => e.WalkSpeed).HasColumnName("walk_speed");
        builder.Property(e => e.AttackDelay).HasColumnName("attack_delay");
        builder.Property(e => e.AttackMotion).HasColumnName("attack_motion");
        builder.Property(e => e.DamageMotion).HasColumnName("damage_motion");
        builder.Property(e => e.DamageTaken).HasColumnName("damage_taken");
        
        builder.Property(e => e.GroupId).HasColumnName("groupid");
        builder.Property(e => e.Title).HasColumnName("title").HasColumnType("text");
        builder.Property(e => e.Ai).HasColumnName("ai").HasMaxLength(50);
        builder.Property(e => e.Class).HasColumnName("class").HasMaxLength(50);
        
        // Mode flags
        builder.Property(e => e.ModeCanmove).HasColumnName("mode_canmove");
        builder.Property(e => e.ModeLooter).HasColumnName("mode_looter");
        builder.Property(e => e.ModeAggressive).HasColumnName("mode_aggressive");
        builder.Property(e => e.ModeAssist).HasColumnName("mode_assist");
        builder.Property(e => e.ModeCastsensoridle).HasColumnName("mode_castsensoridle");
        builder.Property(e => e.ModeNorandomwalk).HasColumnName("mode_norandomwalk");
        builder.Property(e => e.ModeNocast).HasColumnName("mode_nocast");
        builder.Property(e => e.ModeCanattack).HasColumnName("mode_canattack");
        builder.Property(e => e.ModeCastsensorchase).HasColumnName("mode_castsensorchase");
        builder.Property(e => e.ModeChangechase).HasColumnName("mode_changechase");
        builder.Property(e => e.ModeAngry).HasColumnName("mode_angry");
        builder.Property(e => e.ModeChangetargetmelee).HasColumnName("mode_changetargetmelee");
        builder.Property(e => e.ModeChangetargetchase).HasColumnName("mode_changetargetchase");
        builder.Property(e => e.ModeTargetweak).HasColumnName("mode_targetweak");
        builder.Property(e => e.ModeRandomtarget).HasColumnName("mode_randomtarget");
        builder.Property(e => e.ModeIgnoremelee).HasColumnName("mode_ignoremelee");
        builder.Property(e => e.ModeIgnoremagic).HasColumnName("mode_ignoremagic");
        builder.Property(e => e.ModeIgnoreranged).HasColumnName("mode_ignoreranged");
        builder.Property(e => e.ModeMvp).HasColumnName("mode_mvp");
        builder.Property(e => e.ModeIgnoremisc).HasColumnName("mode_ignoremisc");
        builder.Property(e => e.ModeKnockbackimmune).HasColumnName("mode_knockbackimmune");
        builder.Property(e => e.ModeTeleportblock).HasColumnName("mode_teleportblock");
        builder.Property(e => e.ModeFixeditemdrop).HasColumnName("mode_fixeditemdrop");
        builder.Property(e => e.ModeDetector).HasColumnName("mode_detector");
        builder.Property(e => e.ModeStatusimmune).HasColumnName("mode_statusimmune");
        builder.Property(e => e.ModeSkillimmune).HasColumnName("mode_skillimmune");
        
        // MVP Drops
        builder.Property(e => e.Mvpdrop1Item).HasColumnName("mvpdrop1_item").HasMaxLength(50);
        builder.Property(e => e.Mvpdrop1Rate).HasColumnName("mvpdrop1_rate");
        builder.Property(e => e.Mvpdrop1Option).HasColumnName("mvpdrop1_option").HasMaxLength(50);
        builder.Property(e => e.Mvpdrop1Index).HasColumnName("mvpdrop1_index");
        builder.Property(e => e.Mvpdrop2Item).HasColumnName("mvpdrop2_item").HasMaxLength(50);
        builder.Property(e => e.Mvpdrop2Rate).HasColumnName("mvpdrop2_rate");
        builder.Property(e => e.Mvpdrop2Option).HasColumnName("mvpdrop2_option").HasMaxLength(50);
        builder.Property(e => e.Mvpdrop2Index).HasColumnName("mvpdrop2_index");
        builder.Property(e => e.Mvpdrop3Item).HasColumnName("mvpdrop3_item").HasMaxLength(50);
        builder.Property(e => e.Mvpdrop3Rate).HasColumnName("mvpdrop3_rate");
        builder.Property(e => e.Mvpdrop3Option).HasColumnName("mvpdrop3_option").HasMaxLength(50);
        builder.Property(e => e.Mvpdrop3Index).HasColumnName("mvpdrop3_index");
        
        // Regular Drops (10 slots - I'll show the pattern for all)
        builder.Property(e => e.Drop1Item).HasColumnName("drop1_item").HasMaxLength(50);
        builder.Property(e => e.Drop1Rate).HasColumnName("drop1_rate");
        builder.Property(e => e.Drop1Nosteal).HasColumnName("drop1_nosteal");
        builder.Property(e => e.Drop1Option).HasColumnName("drop1_option").HasMaxLength(50);
        builder.Property(e => e.Drop1Index).HasColumnName("drop1_index");
        
        builder.Property(e => e.Drop2Item).HasColumnName("drop2_item").HasMaxLength(50);
        builder.Property(e => e.Drop2Rate).HasColumnName("drop2_rate");
        builder.Property(e => e.Drop2Nosteal).HasColumnName("drop2_nosteal");
        builder.Property(e => e.Drop2Option).HasColumnName("drop2_option").HasMaxLength(50);
        builder.Property(e => e.Drop2Index).HasColumnName("drop2_index");
        
        builder.Property(e => e.Drop3Item).HasColumnName("drop3_item").HasMaxLength(50);
        builder.Property(e => e.Drop3Rate).HasColumnName("drop3_rate");
        builder.Property(e => e.Drop3Nosteal).HasColumnName("drop3_nosteal");
        builder.Property(e => e.Drop3Option).HasColumnName("drop3_option").HasMaxLength(50);
        builder.Property(e => e.Drop3Index).HasColumnName("drop3_index");
        
        builder.Property(e => e.Drop4Item).HasColumnName("drop4_item").HasMaxLength(50);
        builder.Property(e => e.Drop4Rate).HasColumnName("drop4_rate");
        builder.Property(e => e.Drop4Nosteal).HasColumnName("drop4_nosteal");
        builder.Property(e => e.Drop4Option).HasColumnName("drop4_option").HasMaxLength(50);
        builder.Property(e => e.Drop4Index).HasColumnName("drop4_index");
        
        builder.Property(e => e.Drop5Item).HasColumnName("drop5_item").HasMaxLength(50);
        builder.Property(e => e.Drop5Rate).HasColumnName("drop5_rate");
        builder.Property(e => e.Drop5Nosteal).HasColumnName("drop5_nosteal");
        builder.Property(e => e.Drop5Option).HasColumnName("drop5_option").HasMaxLength(50);
        builder.Property(e => e.Drop5Index).HasColumnName("drop5_index");
        
        builder.Property(e => e.Drop6Item).HasColumnName("drop6_item").HasMaxLength(50);
        builder.Property(e => e.Drop6Rate).HasColumnName("drop6_rate");
        builder.Property(e => e.Drop6Nosteal).HasColumnName("drop6_nosteal");
        builder.Property(e => e.Drop6Option).HasColumnName("drop6_option").HasMaxLength(50);
        builder.Property(e => e.Drop6Index).HasColumnName("drop6_index");
        
        builder.Property(e => e.Drop7Item).HasColumnName("drop7_item").HasMaxLength(50);
        builder.Property(e => e.Drop7Rate).HasColumnName("drop7_rate");
        builder.Property(e => e.Drop7Nosteal).HasColumnName("drop7_nosteal");
        builder.Property(e => e.Drop7Option).HasColumnName("drop7_option").HasMaxLength(50);
        builder.Property(e => e.Drop7Index).HasColumnName("drop7_index");
        
        builder.Property(e => e.Drop8Item).HasColumnName("drop8_item").HasMaxLength(50);
        builder.Property(e => e.Drop8Rate).HasColumnName("drop8_rate");
        builder.Property(e => e.Drop8Nosteal).HasColumnName("drop8_nosteal");
        builder.Property(e => e.Drop8Option).HasColumnName("drop8_option").HasMaxLength(50);
        builder.Property(e => e.Drop8Index).HasColumnName("drop8_index");
        
        builder.Property(e => e.Drop9Item).HasColumnName("drop9_item").HasMaxLength(50);
        builder.Property(e => e.Drop9Rate).HasColumnName("drop9_rate");
        builder.Property(e => e.Drop9Nosteal).HasColumnName("drop9_nosteal");
        builder.Property(e => e.Drop9Option).HasColumnName("drop9_option").HasMaxLength(50);
        builder.Property(e => e.Drop9Index).HasColumnName("drop9_index");
        
        builder.Property(e => e.Drop10Item).HasColumnName("drop10_item").HasMaxLength(50);
        builder.Property(e => e.Drop10Rate).HasColumnName("drop10_rate");
        builder.Property(e => e.Drop10Nosteal).HasColumnName("drop10_nosteal");
        builder.Property(e => e.Drop10Option).HasColumnName("drop10_option").HasMaxLength(50);
        builder.Property(e => e.Drop10Index).HasColumnName("drop10_index");
        
        // Indexes
        builder.HasIndex(e => e.NameAegis).IsUnique();
    }
}