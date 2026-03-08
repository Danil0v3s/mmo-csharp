namespace Core.Server.Packets;

public class CharacterInfo
{
    public const int SerializedSize = 175;

    public uint GID { get; set; }
    public long Exp { get; set; }
    public int Money { get; set; }
    public long JobExp { get; set; }
    public int JobLevel { get; set; }
    public int BodyState { get; set; }
    public int HealthState { get; set; }
    public int EffectState { get; set; }
    public int Virtue { get; set; }
    public int Honor { get; set; }
    public short JobPoint { get; set; }
    public long Hp { get; set; }
    public long MaxHp { get; set; }
    public long Sp { get; set; }
    public long MaxSp { get; set; }
    public short Speed { get; set; }
    public short Job { get; set; }
    public short Head { get; set; }
    public short Body { get; set; }
    public short Weapon { get; set; }
    public short Level { get; set; }
    public short SpPoint { get; set; }
    public short Accessory { get; set; }
    public short Shield { get; set; }
    public short Accessory2 { get; set; }
    public short Accessory3 { get; set; }
    public short HeadPalette { get; set; }
    public short BodyPalette { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte Str { get; set; }
    public byte Agi { get; set; }
    public byte Vit { get; set; }
    public byte Int { get; set; }
    public byte Dex { get; set; }
    public byte Luk { get; set; }
    public byte CharNum { get; set; }
    public byte HairColor { get; set; }
    public short IsChangedCharName { get; set; }
    public string MapName { get; set; } = string.Empty;
    public int DelRevDate { get; set; }
    public int RobePalette { get; set; }
    public int ChrSlotChangeCnt { get; set; }
    public int ChrNameChangeCnt { get; set; }
    public byte Sex { get; set; }

    public void Read(BinaryReader reader)
    {
        GID = reader.ReadUInt32();
        
        // PACKETVER >= 20170830: int64 exp
        Exp = reader.ReadInt64();
        
        Money = reader.ReadInt32();
        
        // PACKETVER >= 20170830: int64 jobexp
        JobExp = reader.ReadInt64();
        
        JobLevel = reader.ReadInt32();
        BodyState = reader.ReadInt32();
        HealthState = reader.ReadInt32();
        EffectState = reader.ReadInt32();
        Virtue = reader.ReadInt32();
        Honor = reader.ReadInt32();
        JobPoint = reader.ReadInt16();
        
        // PACKETVER_RE_NUM >= 20211103 || PACKETVER_MAIN_NUM >= 20220330: int64 hp, sp
        Hp = reader.ReadInt64();
        MaxHp = reader.ReadInt64();
        Sp = reader.ReadInt64();
        MaxSp = reader.ReadInt64();
        
        Speed = reader.ReadInt16();
        Job = reader.ReadInt16();
        Head = reader.ReadInt16();
        
        // PACKETVER >= 20141022
        Body = reader.ReadInt16();
        
        Weapon = reader.ReadInt16();
        Level = reader.ReadInt16();
        SpPoint = reader.ReadInt16();
        Accessory = reader.ReadInt16();
        Shield = reader.ReadInt16();
        Accessory2 = reader.ReadInt16();
        Accessory3 = reader.ReadInt16();
        HeadPalette = reader.ReadInt16();
        BodyPalette = reader.ReadInt16();
        
        Name = reader.ReadFixedString(24);
        
        Str = reader.ReadByte();
        Agi = reader.ReadByte();
        Vit = reader.ReadByte();
        Int = reader.ReadByte();
        Dex = reader.ReadByte();
        Luk = reader.ReadByte();
        CharNum = reader.ReadByte();
        HairColor = reader.ReadByte();
        IsChangedCharName = reader.ReadInt16();
        
        // (PACKETVER >= 20100720 && PACKETVER <= 20100727) || PACKETVER >= 20100803
        MapName = reader.ReadFixedString(16);
        
        // PACKETVER >= 20100803
        DelRevDate = reader.ReadInt32();
        
        // PACKETVER >= 20110111
        RobePalette = reader.ReadInt32();
        
        // PACKETVER >= 20110928
        ChrSlotChangeCnt = reader.ReadInt32();
        
        // PACKETVER >= 20111025
        ChrNameChangeCnt = reader.ReadInt32();
        
        // PACKETVER >= 20141016
        Sex = reader.ReadByte();
    }
    
    public void Write(BinaryWriter writer)
    {
        writer.Write(GID);
        writer.Write(Exp);
        writer.Write(Money);
        writer.Write(JobExp);
        writer.Write(JobLevel);
        writer.Write(BodyState);
        writer.Write(HealthState);
        writer.Write(EffectState);
        writer.Write(Virtue);
        writer.Write(Honor);
        writer.Write(JobPoint);
        writer.Write(Hp);
        writer.Write(MaxHp);
        writer.Write(Sp);
        writer.Write(MaxSp);
        writer.Write(Speed);
        writer.Write(Job);
        writer.Write(Head);
        writer.Write(Body);
        writer.Write(Weapon);
        writer.Write(Level);
        writer.Write(SpPoint);
        writer.Write(Accessory);
        writer.Write(Shield);
        writer.Write(Accessory2);
        writer.Write(Accessory3);
        writer.Write(HeadPalette);
        writer.Write(BodyPalette);
        writer.WriteFixedString(Name, 24);
        writer.Write(Str);
        writer.Write(Agi);
        writer.Write(Vit);
        writer.Write(Int);
        writer.Write(Dex);
        writer.Write(Luk);
        writer.Write(CharNum);
        writer.Write(HairColor);
        writer.Write(IsChangedCharName);
        writer.WriteFixedString(MapName, 16);
        writer.Write(DelRevDate);
        writer.Write(RobePalette);
        writer.Write(ChrSlotChangeCnt);
        writer.Write(ChrNameChangeCnt);
        writer.Write(Sex);
    }
    
    public int GetSize()
    {
        return SerializedSize;
    }

    public static void ValidateBlockSize(int length, string fieldName)
    {
        if (length % SerializedSize != 0)
        {
            throw new InvalidOperationException(
                $"{fieldName} length must be a multiple of {SerializedSize} bytes, but was {length}.");
        }
    }

    public static void ValidateSingleSize(int length, string fieldName)
    {
        if (length != SerializedSize)
        {
            throw new InvalidOperationException(
                $"{fieldName} length must be exactly {SerializedSize} bytes, but was {length}.");
        }
    }
}
