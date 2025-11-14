namespace Core.Server.Packets.In.CH;

public class CH_PINCODE_CHANGE : IncomingPacket
{
    public uint AccountId { get; internal set; }
    public string OldPin { get; internal set; } = string.Empty;
    public string NewPin { get; internal set; } = string.Empty;

    public CH_PINCODE_CHANGE() : base(PacketHeader.CH_PINCODE_CHANGE, true)
    {
        OldPin = new string('\0', 4);
        NewPin = new string('\0', 4);
    }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
        AccountId = reader.ReadUInt32();

        // Read old PIN (typically 4 characters)
        OldPin = reader.ReadFixedString(4);

        // Read new PIN (typically 4 characters)
        NewPin = reader.ReadFixedString(4);
    }

    public override int GetSize()
    {
        return sizeof(short) + sizeof(uint) + 4 + 4; // packetType + accountId + oldpin[4] + newpin[4]
    }
}