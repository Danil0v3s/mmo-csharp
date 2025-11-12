namespace Core.Database.Entities;

public class MobEntity
{
    public uint Id { get; set; }
    public string NameAegis { get; set; } = string.Empty;
    public string NameEnglish { get; set; } = string.Empty;
    public string? NameJapanese { get; set; }
    public ushort? Level { get; set; }
    public uint? Hp { get; set; }
    public uint? Sp { get; set; }
    public uint? BaseExp { get; set; }
    public uint? JobExp { get; set; }
    public uint? MvpExp { get; set; }
    public ushort? Attack { get; set; }
    public ushort? Attack2 { get; set; }
    public ushort? Defense { get; set; }
    public ushort? MagicDefense { get; set; }
    public ushort? Resistance { get; set; }
    public ushort? MagicResistance { get; set; }
    
    // Stats
    public ushort? Str { get; set; }
    public ushort? Agi { get; set; }
    public ushort? Vit { get; set; }
    public ushort? Int { get; set; }
    public ushort? Dex { get; set; }
    public ushort? Luk { get; set; }
    
    // Ranges
    public byte? AttackRange { get; set; }
    public byte? SkillRange { get; set; }
    public byte? ChaseRange { get; set; }
    
    // Type info
    public string? Size { get; set; }
    public string? Race { get; set; }
    
    // Race groups (29 different groups)
    public byte? RacegroupGoblin { get; set; }
    public byte? RacegroupKobold { get; set; }
    public byte? RacegroupOrc { get; set; }
    public byte? RacegroupGolem { get; set; }
    public byte? RacegroupGuardian { get; set; }
    public byte? RacegroupNinja { get; set; }
    public byte? RacegroupGvg { get; set; }
    public byte? RacegroupBattlefield { get; set; }
    public byte? RacegroupTreasure { get; set; }
    public byte? RacegroupBiolab { get; set; }
    public byte? RacegroupManuk { get; set; }
    public byte? RacegroupSplendide { get; set; }
    public byte? RacegroupScaraba { get; set; }
    public byte? RacegroupOghAtkDef { get; set; }
    public byte? RacegroupOghHidden { get; set; }
    public byte? RacegroupBio5SwordmanThief { get; set; }
    public byte? RacegroupBio5AcolyteMerchant { get; set; }
    public byte? RacegroupBio5MageArcher { get; set; }
    public byte? RacegroupBio5Mvp { get; set; }
    public byte? RacegroupClocktower { get; set; }
    public byte? RacegroupThanatos { get; set; }
    public byte? RacegroupFaceworm { get; set; }
    public byte? RacegroupHearthunter { get; set; }
    public byte? RacegroupRockridge { get; set; }
    public byte? RacegroupWernerLab { get; set; }
    public byte? RacegroupTempleDemon { get; set; }
    public byte? RacegroupIllusionVampire { get; set; }
    public byte? RacegroupMalangdo { get; set; }
    public byte? RacegroupEp172alpha { get; set; }
    public byte? RacegroupEp172beta { get; set; }
    public byte? RacegroupEp172bath { get; set; }
    public byte? RacegroupIllusionTurtle { get; set; }
    public byte? RacegroupRachelSanctuary { get; set; }
    public byte? RacegroupIllusionLuanda { get; set; }
    public byte? RacegroupIllusionFrozen { get; set; }
    public byte? RacegroupIllusionMoonlight { get; set; }
    public byte? RacegroupEp16Def { get; set; }
    public byte? RacegroupEddaArunafeltz { get; set; }
    public byte? RacegroupLasagna { get; set; }
    public byte? RacegroupGlastHeimAbyss { get; set; }
    public byte? RacegroupDestroyedValkyrieRealm { get; set; }
    public byte? RacegroupEncroachedGephenia { get; set; }
    
    // Element
    public string? Element { get; set; }
    public byte? ElementLevel { get; set; }
    
    // Timing
    public ushort? WalkSpeed { get; set; }
    public ushort? AttackDelay { get; set; }
    public ushort? AttackMotion { get; set; }
    public ushort? DamageMotion { get; set; }
    public ushort? DamageTaken { get; set; }
    
    public ushort? GroupId { get; set; }
    public string? Title { get; set; }
    public string? Ai { get; set; }
    public string? Class { get; set; }
    
    // Mode flags (26 flags)
    public byte? ModeCanmove { get; set; }
    public byte? ModeLooter { get; set; }
    public byte? ModeAggressive { get; set; }
    public byte? ModeAssist { get; set; }
    public byte? ModeCastsensoridle { get; set; }
    public byte? ModeNorandomwalk { get; set; }
    public byte? ModeNocast { get; set; }
    public byte? ModeCanattack { get; set; }
    public byte? ModeCastsensorchase { get; set; }
    public byte? ModeChangechase { get; set; }
    public byte? ModeAngry { get; set; }
    public byte? ModeChangetargetmelee { get; set; }
    public byte? ModeChangetargetchase { get; set; }
    public byte? ModeTargetweak { get; set; }
    public byte? ModeRandomtarget { get; set; }
    public byte? ModeIgnoremelee { get; set; }
    public byte? ModeIgnoremagic { get; set; }
    public byte? ModeIgnoreranged { get; set; }
    public byte? ModeMvp { get; set; }
    public byte? ModeIgnoremisc { get; set; }
    public byte? ModeKnockbackimmune { get; set; }
    public byte? ModeTeleportblock { get; set; }
    public byte? ModeFixeditemdrop { get; set; }
    public byte? ModeDetector { get; set; }
    public byte? ModeStatusimmune { get; set; }
    public byte? ModeSkillimmune { get; set; }
    
    // MVP Drops (3 slots)
    public string? Mvpdrop1Item { get; set; }
    public ushort? Mvpdrop1Rate { get; set; }
    public string? Mvpdrop1Option { get; set; }
    public byte? Mvpdrop1Index { get; set; }
    public string? Mvpdrop2Item { get; set; }
    public ushort? Mvpdrop2Rate { get; set; }
    public string? Mvpdrop2Option { get; set; }
    public byte? Mvpdrop2Index { get; set; }
    public string? Mvpdrop3Item { get; set; }
    public ushort? Mvpdrop3Rate { get; set; }
    public string? Mvpdrop3Option { get; set; }
    public byte? Mvpdrop3Index { get; set; }
    
    // Regular Drops (10 slots)
    public string? Drop1Item { get; set; }
    public ushort? Drop1Rate { get; set; }
    public byte? Drop1Nosteal { get; set; }
    public string? Drop1Option { get; set; }
    public byte? Drop1Index { get; set; }
    public string? Drop2Item { get; set; }
    public ushort? Drop2Rate { get; set; }
    public byte? Drop2Nosteal { get; set; }
    public string? Drop2Option { get; set; }
    public byte? Drop2Index { get; set; }
    public string? Drop3Item { get; set; }
    public ushort? Drop3Rate { get; set; }
    public byte? Drop3Nosteal { get; set; }
    public string? Drop3Option { get; set; }
    public byte? Drop3Index { get; set; }
    public string? Drop4Item { get; set; }
    public ushort? Drop4Rate { get; set; }
    public byte? Drop4Nosteal { get; set; }
    public string? Drop4Option { get; set; }
    public byte? Drop4Index { get; set; }
    public string? Drop5Item { get; set; }
    public ushort? Drop5Rate { get; set; }
    public byte? Drop5Nosteal { get; set; }
    public string? Drop5Option { get; set; }
    public byte? Drop5Index { get; set; }
    public string? Drop6Item { get; set; }
    public ushort? Drop6Rate { get; set; }
    public byte? Drop6Nosteal { get; set; }
    public string? Drop6Option { get; set; }
    public byte? Drop6Index { get; set; }
    public string? Drop7Item { get; set; }
    public ushort? Drop7Rate { get; set; }
    public byte? Drop7Nosteal { get; set; }
    public string? Drop7Option { get; set; }
    public byte? Drop7Index { get; set; }
    public string? Drop8Item { get; set; }
    public ushort? Drop8Rate { get; set; }
    public byte? Drop8Nosteal { get; set; }
    public string? Drop8Option { get; set; }
    public byte? Drop8Index { get; set; }
    public string? Drop9Item { get; set; }
    public ushort? Drop9Rate { get; set; }
    public byte? Drop9Nosteal { get; set; }
    public string? Drop9Option { get; set; }
    public byte? Drop9Index { get; set; }
    public string? Drop10Item { get; set; }
    public ushort? Drop10Rate { get; set; }
    public byte? Drop10Nosteal { get; set; }
    public string? Drop10Option { get; set; }
    public byte? Drop10Index { get; set; }
}