namespace Core.Database.Entities;

public class CharEntity
{
    public int CharId { get; set; }
    public int AccountId { get; set; }
    public byte CharNum { get; set; }
    public string Name { get; set; } = string.Empty;
    public ushort Class { get; set; }
    public ushort BaseLevel { get; set; } = 1;
    public ushort JobLevel { get; set; } = 1;
    public ulong BaseExp { get; set; }
    public ulong JobExp { get; set; }
    public uint Zeny { get; set; }
    public ushort Str { get; set; }
    public ushort Agi { get; set; }
    public ushort Vit { get; set; }
    public ushort Int { get; set; }
    public ushort Dex { get; set; }
    public ushort Luk { get; set; }
    public ushort Pow { get; set; }
    public ushort Sta { get; set; }
    public ushort Wis { get; set; }
    public ushort Spl { get; set; }
    public ushort Con { get; set; }
    public ushort Crt { get; set; }
    public uint MaxHp { get; set; }
    public uint Hp { get; set; }
    public uint MaxSp { get; set; }
    public uint Sp { get; set; }
    public uint MaxAp { get; set; }
    public uint Ap { get; set; }
    public uint StatusPoint { get; set; }
    public uint SkillPoint { get; set; }
    public uint TraitPoint { get; set; }
    public int Option { get; set; }
    public sbyte Karma { get; set; }
    public short Manner { get; set; }
    public int PartyId { get; set; }
    public int GuildId { get; set; }
    public int PetId { get; set; }
    public int HomunId { get; set; }
    public int ElementalId { get; set; }
    public byte Hair { get; set; }
    public ushort HairColor { get; set; }
    public ushort ClothesColor { get; set; }
    public ushort Body { get; set; }
    public ushort Weapon { get; set; }
    public ushort Shield { get; set; }
    public ushort HeadTop { get; set; }
    public ushort HeadMid { get; set; }
    public ushort HeadBottom { get; set; }
    public ushort Robe { get; set; }
    public string LastMap { get; set; } = string.Empty;
    public ushort LastX { get; set; } = 53;
    public ushort LastY { get; set; } = 111;
    public uint LastInstanceId { get; set; }
    public string SaveMap { get; set; } = string.Empty;
    public ushort SaveX { get; set; } = 53;
    public ushort SaveY { get; set; } = 111;
    public int PartnerId { get; set; }
    public short Online { get; set; }
    public int Father { get; set; }
    public int Mother { get; set; }
    public int Child { get; set; }
    public uint Fame { get; set; }
    public ushort Rename { get; set; }
    public uint DeleteDate { get; set; }
    public uint Moves { get; set; }
    public uint UnbanTime { get; set; }
    public byte Font { get; set; }
    public uint UniqueItemCounter { get; set; }
    public string Sex { get; set; } = "M";
    public byte HotkeyRowshift { get; set; }
    public byte HotkeyRowshift2 { get; set; }
    public int ClanId { get; set; }
    public DateTime? LastLogin { get; set; }
    public uint TitleId { get; set; }
    public byte ShowEquip { get; set; }
    public short InventorySlots { get; set; } = 100;
    public byte BodyDirection { get; set; }
    public byte DisableCall { get; set; }
    public byte DisablePartyInvite { get; set; }
    public byte DisableShowCostumes { get; set; }
    
    // Navigation properties
    public LoginEntity? Account { get; set; }
    public PartyEntity? Party { get; set; }
    public GuildEntity? Guild { get; set; }
    public ClanEntity? Clan { get; set; }
    public ICollection<InventoryEntity> Inventories { get; set; } = new List<InventoryEntity>();
    public ICollection<CartInventoryEntity> CartInventories { get; set; } = new List<CartInventoryEntity>();
    public ICollection<SkillEntity> Skills { get; set; } = new List<SkillEntity>();
    public ICollection<QuestEntity> Quests { get; set; } = new List<QuestEntity>();
    public ICollection<AchievementEntity> Achievements { get; set; } = new List<AchievementEntity>();
    public ICollection<FriendEntity> Friends { get; set; } = new List<FriendEntity>();
    public ICollection<HotkeyEntity> Hotkeys { get; set; } = new List<HotkeyEntity>();
    public ICollection<MemoEntity> Memos { get; set; } = new List<MemoEntity>();
    public ICollection<BonusScriptEntity> BonusScripts { get; set; } = new List<BonusScriptEntity>();
}

