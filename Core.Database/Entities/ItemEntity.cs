namespace Core.Database.Entities;

public class ItemEntity
{
    public uint Id { get; set; }
    public string NameAegis { get; set; } = string.Empty;
    public string NameEnglish { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Subtype { get; set; }
    public uint? PriceBuy { get; set; }
    public uint? PriceSell { get; set; }
    public ushort? Weight { get; set; }
    public ushort? Attack { get; set; }
    public ushort? MagicAttack { get; set; }
    public ushort? Defense { get; set; }
    public byte? Range { get; set; }
    public byte? Slots { get; set; }
    
    // Job restrictions
    public byte? JobAll { get; set; }
    public byte? JobAcolyte { get; set; }
    public byte? JobAlchemist { get; set; }
    public byte? JobArcher { get; set; }
    public byte? JobAssassin { get; set; }
    public byte? JobBarddancer { get; set; }
    public byte? JobBlacksmith { get; set; }
    public byte? JobCrusader { get; set; }
    public byte? JobGunslinger { get; set; }
    public byte? JobHunter { get; set; }
    public byte? JobKagerouoboro { get; set; }
    public byte? JobKnight { get; set; }
    public byte? JobMage { get; set; }
    public byte? JobMerchant { get; set; }
    public byte? JobMonk { get; set; }
    public byte? JobNinja { get; set; }
    public byte? JobNovice { get; set; }
    public byte? JobPriest { get; set; }
    public byte? JobRebellion { get; set; }
    public byte? JobRogue { get; set; }
    public byte? JobSage { get; set; }
    public byte? JobSoullinker { get; set; }
    public byte? JobSpiritHandler { get; set; }
    public byte? JobStargladiator { get; set; }
    public byte? JobSummoner { get; set; }
    public byte? JobSupernovice { get; set; }
    public byte? JobSwordman { get; set; }
    public byte? JobTaekwon { get; set; }
    public byte? JobThief { get; set; }
    public byte? JobWizard { get; set; }
    
    // Class restrictions
    public byte? ClassAll { get; set; }
    public byte? ClassNormal { get; set; }
    public byte? ClassUpper { get; set; }
    public byte? ClassBaby { get; set; }
    public byte? ClassThird { get; set; }
    public byte? ClassThirdUpper { get; set; }
    public byte? ClassThirdBaby { get; set; }
    public byte? ClassFourth { get; set; }
    
    public string? Gender { get; set; }
    
    // Equipment locations
    public byte? LocationHeadTop { get; set; }
    public byte? LocationHeadMid { get; set; }
    public byte? LocationHeadLow { get; set; }
    public byte? LocationArmor { get; set; }
    public byte? LocationRightHand { get; set; }
    public byte? LocationLeftHand { get; set; }
    public byte? LocationGarment { get; set; }
    public byte? LocationShoes { get; set; }
    public byte? LocationRightAccessory { get; set; }
    public byte? LocationLeftAccessory { get; set; }
    public byte? LocationCostumeHeadTop { get; set; }
    public byte? LocationCostumeHeadMid { get; set; }
    public byte? LocationCostumeHeadLow { get; set; }
    public byte? LocationCostumeGarment { get; set; }
    public byte? LocationAmmo { get; set; }
    public byte? LocationShadowArmor { get; set; }
    public byte? LocationShadowWeapon { get; set; }
    public byte? LocationShadowShield { get; set; }
    public byte? LocationShadowShoes { get; set; }
    public byte? LocationShadowRightAccessory { get; set; }
    public byte? LocationShadowLeftAccessory { get; set; }
    
    // Equipment properties
    public byte? WeaponLevel { get; set; }
    public byte? ArmorLevel { get; set; }
    public ushort? EquipLevelMin { get; set; }
    public ushort? EquipLevelMax { get; set; }
    public byte? Refineable { get; set; }
    public byte? Gradable { get; set; }
    public ushort? View { get; set; }
    public string? AliasName { get; set; }
    
    // Flags
    public byte? FlagBuyingstore { get; set; }
    public byte? FlagDeadbranch { get; set; }
    public byte? FlagContainer { get; set; }
    public byte? FlagUniqueid { get; set; }
    public byte? FlagBindonequip { get; set; }
    public byte? FlagDropannounce { get; set; }
    public byte? FlagNoconsume { get; set; }
    public string? FlagDropeffect { get; set; }
    
    // Delay
    public ulong? DelayDuration { get; set; }
    public string? DelayStatus { get; set; }
    
    // Stack
    public ushort? StackAmount { get; set; }
    public byte? StackInventory { get; set; }
    public byte? StackCart { get; set; }
    public byte? StackStorage { get; set; }
    public byte? StackGuildstorage { get; set; }
    
    // Usage restrictions
    public ushort? NouseOverride { get; set; }
    public byte? NouseSitting { get; set; }
    
    // Trade restrictions
    public ushort? TradeOverride { get; set; }
    public byte? TradeNodrop { get; set; }
    public byte? TradeNotrade { get; set; }
    public byte? TradeTradepartner { get; set; }
    public byte? TradeNosell { get; set; }
    public byte? TradeNocart { get; set; }
    public byte? TradeNostorage { get; set; }
    public byte? TradeNoguildstorage { get; set; }
    public byte? TradeNomail { get; set; }
    public byte? TradeNoauction { get; set; }
    
    // Scripts
    public string? Script { get; set; }
    public string? EquipScript { get; set; }
    public string? UnequipScript { get; set; }
}