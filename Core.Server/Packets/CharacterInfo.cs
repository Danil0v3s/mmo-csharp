namespace Core.Server.Packets;

public class CharacterInfo
{
    public uint GID { get; internal set; } // or init; depending on usage
    public long Exp { get; internal set; }
    public int Money { get; internal set; }
    public long JobExp { get; internal set; }
    public int JobLevel { get; internal set; }
    public int BodyState { get; internal set; }
    public int HealthState { get; internal set; }
    public int EffectState { get; internal set; }
    public int Virtue { get; internal set; }
    public int Honor { get; internal set; }
    public short JobPoint { get; internal set; }
    public long Hp { get; internal set; }
    public long MaxHp { get; internal set; }
    public long Sp { get; internal set; }
    public long MaxSp { get; internal set; }
    public short Speed { get; internal set; }
    public short Job { get; internal set; }
    public short Head { get; internal set; }
    public short Body { get; internal set; }
    public short Weapon { get; internal set; }
    public short Level { get; internal set; }
    public short SpPoint { get; internal set; }
    public short Accessory { get; internal set; }
    public short Shield { get; internal set; }
    public short Accessory2 { get; internal set; }
    public short Accessory3 { get; internal set; }
    public short HeadPalette { get; internal set; }
    public short BodyPalette { get; internal set; }
    public string Name { get; internal set; } = string.Empty;
    public byte Str { get; internal set; }
    public byte Agi { get; internal set; }
    public byte Vit { get; internal set; }
    public byte Int { get; internal set; }
    public byte Dex { get; internal set; }
    public byte Luk { get; internal set; }
    public byte CharNum { get; internal set; }
    public byte HairColor { get; internal set; }
    public short IsChangedCharName { get; internal set; }
    public string MapName { get; internal set; } = string.Empty;
    public int DelRevDate { get; internal set; }
    public int RobePalette { get; internal set; }
    public int ChrSlotChangeCnt { get; internal set; }
    public int ChrNameChangeCnt { get; internal set; }
    public byte Sex { get; internal set; }

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
        // Size calculation for CHARACTER_INFO struct
        int size = 0;
        size += sizeof(uint); // GID
        size += sizeof(long); // Exp (int64 for PACKETVER >= 20170830)
        size += sizeof(int); // Money
        size += sizeof(long); // JobExp (int64 for PACKETVER >= 20170830)
        size += sizeof(int); // JobLevel
        size += sizeof(int); // BodyState
        size += sizeof(int); // HealthState
        size += sizeof(int); // EffectState
        size += sizeof(int); // Virtue
        size += sizeof(int); // Honor
        size += sizeof(short); // JobPoint
        size += sizeof(long); // Hp (int64 for >= 20220330)
        size += sizeof(long); // MaxHp
        size += sizeof(long); // Sp
        size += sizeof(long); // MaxSp
        size += sizeof(short); // Speed
        size += sizeof(short); // Job
        size += sizeof(short); // Head
        size += sizeof(short); // Body (PACKETVER >= 20141022)
        size += sizeof(short); // Weapon
        size += sizeof(short); // Level
        size += sizeof(short); // SpPoint
        size += sizeof(short); // Accessory
        size += sizeof(short); // Shield
        size += sizeof(short); // Accessory2
        size += sizeof(short); // Accessory3
        size += sizeof(short); // HeadPalette
        size += sizeof(short); // BodyPalette
        size += 24; // Name[24]
        size += sizeof(byte); // Str
        size += sizeof(byte); // Agi
        size += sizeof(byte); // Vit
        size += sizeof(byte); // Int
        size += sizeof(byte); // Dex
        size += sizeof(byte); // Luk
        size += sizeof(byte); // CharNum
        size += sizeof(byte); // HairColor
        size += sizeof(short); // IsChangedCharName
        size += 16; // MapName[16] (PACKETVER >= 20100803)
        size += sizeof(int); // DelRevDate (PACKETVER >= 20100803)
        size += sizeof(int); // RobePalette (PACKETVER >= 20110111)
        size += sizeof(int); // ChrSlotChangeCnt (PACKETVER >= 20110928)
        size += sizeof(int); // ChrNameChangeCnt (PACKETVER >= 20111025)
        size += sizeof(byte); // Sex (PACKETVER >= 20141016)

        return size;
    }
}