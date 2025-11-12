namespace Core.Database.Entities;

public class GuildEntity
{
    public int GuildId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CharId { get; set; }
    public string Master { get; set; } = string.Empty;
    public byte GuildLv { get; set; }
    public byte ConnectMember { get; set; }
    public byte MaxMember { get; set; }
    public ushort AverageLv { get; set; } = 1;
    public ulong Exp { get; set; }
    public ulong NextExp { get; set; }
    public byte SkillPoint { get; set; }
    public string Mes1 { get; set; } = string.Empty;
    public string Mes2 { get; set; } = string.Empty;
    public uint EmblemLen { get; set; }
    public uint EmblemId { get; set; }
    public byte[]? EmblemData { get; set; }
    public DateTime? LastMasterChange { get; set; }
    
    // Navigation properties
    public ICollection<GuildMemberEntity> Members { get; set; } = new List<GuildMemberEntity>();
    public ICollection<GuildAllianceEntity> Alliances { get; set; } = new List<GuildAllianceEntity>();
    public ICollection<GuildPositionEntity> Positions { get; set; } = new List<GuildPositionEntity>();
    public ICollection<GuildSkillEntity> Skills { get; set; } = new List<GuildSkillEntity>();
    public ICollection<GuildStorageEntity> Storage { get; set; } = new List<GuildStorageEntity>();
    public ICollection<CharEntity> Characters { get; set; } = new List<CharEntity>();
}

