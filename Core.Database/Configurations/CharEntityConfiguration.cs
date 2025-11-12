using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Database.Configurations;

public class CharEntityConfiguration : IEntityTypeConfiguration<CharEntity>
{
    public void Configure(EntityTypeBuilder<CharEntity> builder)
    {
        builder.ToTable("char");
        
        builder.HasKey(e => e.CharId);
        
        builder.Property(e => e.CharId).HasColumnName("char_id");
        builder.Property(e => e.AccountId).HasColumnName("account_id");
        builder.Property(e => e.CharNum).HasColumnName("char_num");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(30).IsRequired();
        builder.Property(e => e.Class).HasColumnName("class");
        builder.Property(e => e.BaseLevel).HasColumnName("base_level").HasDefaultValue((ushort)1);
        builder.Property(e => e.JobLevel).HasColumnName("job_level").HasDefaultValue((ushort)1);
        builder.Property(e => e.BaseExp).HasColumnName("base_exp");
        builder.Property(e => e.JobExp).HasColumnName("job_exp");
        builder.Property(e => e.Zeny).HasColumnName("zeny");
        builder.Property(e => e.Str).HasColumnName("str");
        builder.Property(e => e.Agi).HasColumnName("agi");
        builder.Property(e => e.Vit).HasColumnName("vit");
        builder.Property(e => e.Int).HasColumnName("int");
        builder.Property(e => e.Dex).HasColumnName("dex");
        builder.Property(e => e.Luk).HasColumnName("luk");
        builder.Property(e => e.Pow).HasColumnName("pow");
        builder.Property(e => e.Sta).HasColumnName("sta");
        builder.Property(e => e.Wis).HasColumnName("wis");
        builder.Property(e => e.Spl).HasColumnName("spl");
        builder.Property(e => e.Con).HasColumnName("con");
        builder.Property(e => e.Crt).HasColumnName("crt");
        builder.Property(e => e.MaxHp).HasColumnName("max_hp");
        builder.Property(e => e.Hp).HasColumnName("hp");
        builder.Property(e => e.MaxSp).HasColumnName("max_sp");
        builder.Property(e => e.Sp).HasColumnName("sp");
        builder.Property(e => e.MaxAp).HasColumnName("max_ap");
        builder.Property(e => e.Ap).HasColumnName("ap");
        builder.Property(e => e.StatusPoint).HasColumnName("status_point");
        builder.Property(e => e.SkillPoint).HasColumnName("skill_point");
        builder.Property(e => e.TraitPoint).HasColumnName("trait_point");
        builder.Property(e => e.Option).HasColumnName("option");
        builder.Property(e => e.Karma).HasColumnName("karma");
        builder.Property(e => e.Manner).HasColumnName("manner");
        builder.Property(e => e.PartyId).HasColumnName("party_id");
        builder.Property(e => e.GuildId).HasColumnName("guild_id");
        builder.Property(e => e.PetId).HasColumnName("pet_id");
        builder.Property(e => e.HomunId).HasColumnName("homun_id");
        builder.Property(e => e.ElementalId).HasColumnName("elemental_id");
        builder.Property(e => e.Hair).HasColumnName("hair");
        builder.Property(e => e.HairColor).HasColumnName("hair_color");
        builder.Property(e => e.ClothesColor).HasColumnName("clothes_color");
        builder.Property(e => e.Body).HasColumnName("body");
        builder.Property(e => e.Weapon).HasColumnName("weapon");
        builder.Property(e => e.Shield).HasColumnName("shield");
        builder.Property(e => e.HeadTop).HasColumnName("head_top");
        builder.Property(e => e.HeadMid).HasColumnName("head_mid");
        builder.Property(e => e.HeadBottom).HasColumnName("head_bottom");
        builder.Property(e => e.Robe).HasColumnName("robe");
        builder.Property(e => e.LastMap).HasColumnName("last_map").HasMaxLength(11);
        builder.Property(e => e.LastX).HasColumnName("last_x").HasDefaultValue((ushort)53);
        builder.Property(e => e.LastY).HasColumnName("last_y").HasDefaultValue((ushort)111);
        builder.Property(e => e.LastInstanceId).HasColumnName("last_instanceid");
        builder.Property(e => e.SaveMap).HasColumnName("save_map").HasMaxLength(11);
        builder.Property(e => e.SaveX).HasColumnName("save_x").HasDefaultValue((ushort)53);
        builder.Property(e => e.SaveY).HasColumnName("save_y").HasDefaultValue((ushort)111);
        builder.Property(e => e.PartnerId).HasColumnName("partner_id");
        builder.Property(e => e.Online).HasColumnName("online");
        builder.Property(e => e.Father).HasColumnName("father");
        builder.Property(e => e.Mother).HasColumnName("mother");
        builder.Property(e => e.Child).HasColumnName("child");
        builder.Property(e => e.Fame).HasColumnName("fame");
        builder.Property(e => e.Rename).HasColumnName("rename");
        builder.Property(e => e.DeleteDate).HasColumnName("delete_date");
        builder.Property(e => e.Moves).HasColumnName("moves");
        builder.Property(e => e.UnbanTime).HasColumnName("unban_time");
        builder.Property(e => e.Font).HasColumnName("font");
        builder.Property(e => e.UniqueItemCounter).HasColumnName("uniqueitem_counter");
        builder.Property(e => e.Sex).HasColumnName("sex").HasConversion<string>().HasMaxLength(1);
        builder.Property(e => e.HotkeyRowshift).HasColumnName("hotkey_rowshift");
        builder.Property(e => e.HotkeyRowshift2).HasColumnName("hotkey_rowshift2");
        builder.Property(e => e.ClanId).HasColumnName("clan_id");
        builder.Property(e => e.LastLogin).HasColumnName("last_login");
        builder.Property(e => e.TitleId).HasColumnName("title_id");
        builder.Property(e => e.ShowEquip).HasColumnName("show_equip");
        builder.Property(e => e.InventorySlots).HasColumnName("inventory_slots").HasDefaultValue((short)100);
        builder.Property(e => e.BodyDirection).HasColumnName("body_direction");
        builder.Property(e => e.DisableCall).HasColumnName("disable_call");
        builder.Property(e => e.DisablePartyInvite).HasColumnName("disable_partyinvite");
        builder.Property(e => e.DisableShowCostumes).HasColumnName("disable_showcostumes");
        
        builder.HasIndex(e => e.Name).IsUnique().HasDatabaseName("name_key");
        builder.HasIndex(e => e.AccountId).HasDatabaseName("account_id");
        builder.HasIndex(e => e.PartyId).HasDatabaseName("party_id");
        builder.HasIndex(e => e.GuildId).HasDatabaseName("guild_id");
        builder.HasIndex(e => e.Online).HasDatabaseName("online");
        
        // Relationships
        builder.HasOne(e => e.Party)
            .WithMany(p => p.Members)
            .HasForeignKey(e => e.PartyId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.Guild)
            .WithMany(g => g.Characters)
            .HasForeignKey(e => e.GuildId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.Clan)
            .WithMany(c => c.Members)
            .HasForeignKey(e => e.ClanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

