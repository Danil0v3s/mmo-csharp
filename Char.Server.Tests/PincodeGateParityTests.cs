using Char.Server;
using Core.Server.Packets;

namespace Char.Server.Tests;

public class PincodeGateParityTests
{
    [Theory]
    [InlineData(PacketHeader.CH_REQ_TO_CONNECT)]
    [InlineData(PacketHeader.CH_KEEP_ALIVE)]
    [InlineData(PacketHeader.CH_PINCODE_CHECK)]
    [InlineData(PacketHeader.CH_PINCODE_CHANGE)]
    [InlineData(PacketHeader.CH_REQ_PINCODE_WINDOW)]
    [InlineData(PacketHeader.CH_REQ_CHARLIST)]
    public void IsAllowedBeforePincodeVerification_ShouldAllowExpectedHeaders(PacketHeader header)
    {
        Assert.True(CharServerImpl.IsAllowedBeforePincodeVerification(header));
    }

    [Fact]
    public void IsAllowedBeforePincodeVerification_ShouldRejectSelectChar()
    {
        Assert.False(CharServerImpl.IsAllowedBeforePincodeVerification(PacketHeader.CH_SELECT_CHAR));
    }

    [Fact]
    public void ShouldRejectForPincodeGate_WhenPincodeDisabled_ShouldNotReject()
    {
        Assert.False(CharServerImpl.ShouldRejectForPincodeGate(
            pincodeEnabled: false,
            pincodeVerified: false,
            pincode: "1234",
            header: PacketHeader.CH_SELECT_CHAR));
    }

    [Fact]
    public void ShouldRejectForPincodeGate_WhenNoPinSet_ShouldNotReject()
    {
        Assert.False(CharServerImpl.ShouldRejectForPincodeGate(
            pincodeEnabled: true,
            pincodeVerified: false,
            pincode: string.Empty,
            header: PacketHeader.CH_SELECT_CHAR));
    }

    [Fact]
    public void ShouldRejectForPincodeGate_WhenPinSetAndNotVerified_ShouldRejectBlockedPacket()
    {
        Assert.True(CharServerImpl.ShouldRejectForPincodeGate(
            pincodeEnabled: true,
            pincodeVerified: false,
            pincode: "1234",
            header: PacketHeader.CH_SELECT_CHAR));
    }

    [Fact]
    public void ShouldRejectForPincodeGate_WhenPinSetAndNotVerified_ShouldAllowListedPacket()
    {
        Assert.False(CharServerImpl.ShouldRejectForPincodeGate(
            pincodeEnabled: true,
            pincodeVerified: false,
            pincode: "1234",
            header: PacketHeader.CH_REQ_CHARLIST));
    }
}
