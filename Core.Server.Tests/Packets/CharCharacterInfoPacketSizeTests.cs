using Core.Server.Packets;

namespace Core.Server.Tests.Packets;

public class CharCharacterInfoPacketSizeTests
{
    [Fact]
    public void CharacterInfo_GetSize_Is175Bytes()
    {
        Assert.Equal(175, CharacterInfo.SerializedSize);
        Assert.Equal(175, new CharacterInfo().GetSize());
    }
}
