using Core.Database.Entities;
using PacketCharacterInfo = Core.Server.Packets.CharacterInfo;

namespace Char.Server.Services;

internal static class CharacterPacketSerialization
{
    private const short DefaultWalkSpeed = 150;

    public static byte[] SerializeCharacters(IEnumerable<CharEntity> characters)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        foreach (var character in characters)
        {
            SerializeCharacter(writer, character);
        }

        return ms.ToArray();
    }

    public static byte[] SerializeCharacter(CharEntity character)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        SerializeCharacter(writer, character);
        return ms.ToArray();
    }

    private static void SerializeCharacter(BinaryWriter writer, CharEntity character)
    {
        ToPacketCharacterInfo(character).Write(writer);
    }

    private static PacketCharacterInfo ToPacketCharacterInfo(CharEntity character)
    {
        return new PacketCharacterInfo
        {
            GID = ClampUInt(character.CharId),
            Exp = ClampLong(character.BaseExp),
            Money = ClampInt(character.Zeny),
            JobExp = ClampLong(character.JobExp),
            JobLevel = character.JobLevel,
            BodyState = 0,
            HealthState = 0,
            EffectState = character.Option,
            Virtue = character.Karma,
            Honor = character.Manner,
            JobPoint = ClampShort(character.StatusPoint),
            Hp = character.Hp,
            MaxHp = character.MaxHp,
            Sp = character.Sp,
            MaxSp = character.MaxSp,
            Speed = DefaultWalkSpeed,
            Job = ClampShort(character.Class),
            Head = ClampShort(character.Hair),
            Body = NormalizeBodyStyle(character.Body),
            Weapon = ShouldHideWeapon(character.Option) ? (short)0 : ClampShort(character.Weapon),
            Level = ClampShort(character.BaseLevel),
            SpPoint = ClampShort(character.SkillPoint),
            Accessory = ClampShort(character.HeadBottom),
            Shield = ClampShort(character.Shield),
            Accessory2 = ClampShort(character.HeadTop),
            Accessory3 = ClampShort(character.HeadMid),
            HeadPalette = ClampShort(character.HairColor),
            BodyPalette = ClampShort(character.ClothesColor),
            Name = character.Name,
            Str = ClampByte(character.Str),
            Agi = ClampByte(character.Agi),
            Vit = ClampByte(character.Vit),
            Int = ClampByte(character.Int),
            Dex = ClampByte(character.Dex),
            Luk = ClampByte(character.Luk),
            CharNum = character.CharNum,
            HairColor = ClampByte(character.HairColor),
            IsChangedCharName = character.Rename > 0 ? (short)0 : (short)1,
            MapName = character.LastMap,
            DelRevDate = ClampInt(character.DeleteDate),
            RobePalette = character.Robe,
            ChrSlotChangeCnt = ClampInt(character.Moves),
            ChrNameChangeCnt = character.Rename > 0 ? 1 : 0,
            Sex = ToSex(character.Sex)
        };
    }

    private static bool ShouldHideWeapon(int option)
    {
        const int RidingMask = 0x20 | 0x80000 | 0x100000 | 0x200000 | 0x400000 |
                               0x800000 | 0x1000000 | 0x2000000 | 0x4000000 | 0x8000000;
        return (option & RidingMask) != 0;
    }

    private static short NormalizeBodyStyle(ushort body) => body > 0 ? (short)1 : (short)0;

    private static byte ToSex(string sex) =>
        string.Equals(sex, "F", StringComparison.OrdinalIgnoreCase) ? (byte)0 : (byte)1;

    private static uint ClampUInt(int value) => value < 0 ? 0u : (uint)value;

    private static long ClampLong(ulong value) => (long)Math.Min(value, (ulong)long.MaxValue);

    private static int ClampInt(uint value) => (int)Math.Min(value, (uint)int.MaxValue);

    private static short ClampShort(uint value) => (short)Math.Min(value, (uint)short.MaxValue);

    private static short ClampShort(ushort value) => (short)Math.Min(value, (ushort)short.MaxValue);

    private static short ClampShort(byte value) => value;

    private static byte ClampByte(ushort value) => (byte)Math.Min(value, (ushort)byte.MaxValue);
}
