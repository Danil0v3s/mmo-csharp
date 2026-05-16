using Char.Server.Services;
using Core.Server.Network;
using Core.Server.Packets;
using Microsoft.Extensions.Logging;

namespace Char.Server.Tests.Services;

public class PincodeStateTests
{
    [Fact]
    public void ComputeStartState_WhenDisabled_ReturnsPassedOrDisabled()
    {
        var session = BuildSession();
        session.Pincode = "1234";

        var state = PincodeFlowSupport.ComputeStartState(session, new PincodeConfiguration { Enabled = false });

        Assert.Equal(PincodeState.PassedOrDisabled, state);
    }

    [Fact]
    public void ComputeStartState_WhenNoPincodeAndForceTrue_ReturnsNew()
    {
        var session = BuildSession();
        session.Pincode = string.Empty;

        var state = PincodeFlowSupport.ComputeStartState(
            session,
            new PincodeConfiguration { Enabled = true, Force = true });

        Assert.Equal(PincodeState.New, state);
    }

    [Fact]
    public void ComputeStartState_WhenNoPincodeAndForceFalse_ReturnsPassedOrDisabled()
    {
        var session = BuildSession();
        session.Pincode = string.Empty;

        var state = PincodeFlowSupport.ComputeStartState(
            session,
            new PincodeConfiguration { Enabled = true, Force = false });

        Assert.Equal(PincodeState.PassedOrDisabled, state);
    }

    [Fact]
    public void ComputeStartState_WhenPincodeExpired_ReturnsMustChange()
    {
        var session = BuildSession();
        session.Pincode = "1234";
        // ChangeTime expired: last change was 100s ago, threshold is 50s
        session.PincodeChangeUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 100;

        var state = PincodeFlowSupport.ComputeStartState(
            session,
            new PincodeConfiguration { Enabled = true, ChangeTime = 50 });

        Assert.Equal(PincodeState.MustChange, state);
    }

    [Fact]
    public void ComputeStartState_WhenPincodeNotYetExpired_ReturnsAsk()
    {
        var session = BuildSession();
        session.Pincode = "1234";
        // ChangeTime not yet reached
        session.PincodeChangeUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 10;

        var state = PincodeFlowSupport.ComputeStartState(
            session,
            new PincodeConfiguration { Enabled = true, ChangeTime = 3600 });

        Assert.Equal(PincodeState.Ask, state);
    }

    [Fact]
    public void ComputeStartState_WhenPincodeChangeTimeZero_NeverExpires()
    {
        var session = BuildSession();
        session.Pincode = "1234";
        // Even with ancient timestamp, ChangeTime=0 means no expiration
        session.PincodeChangeUnixTime = 0;

        var state = PincodeFlowSupport.ComputeStartState(
            session,
            new PincodeConfiguration { Enabled = true, ChangeTime = 0 });

        Assert.Equal(PincodeState.Ask, state);
    }

    [Fact]
    public void ComputeStartState_WhenPincodeVerified_ReturnsPassedOrDisabled()
    {
        var session = BuildSession();
        session.Pincode = "1234";
        session.PincodeVerified = true;
        session.PincodeChangeUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var state = PincodeFlowSupport.ComputeStartState(
            session,
            new PincodeConfiguration { Enabled = true, ChangeTime = 3600 });

        Assert.Equal(PincodeState.PassedOrDisabled, state);
    }

    [Fact]
    public void ComputeWindowState_WhenNoPincode_ReturnsNew()
    {
        var session = BuildSession();
        session.Pincode = string.Empty;

        Assert.Equal(PincodeState.New, PincodeFlowSupport.ComputeWindowState(session));
    }

    [Fact]
    public void ComputeWindowState_WhenPincodeExists_ReturnsAsk()
    {
        var session = BuildSession();
        session.Pincode = "1234";

        Assert.Equal(PincodeState.Ask, PincodeFlowSupport.ComputeWindowState(session));
    }

    private static CharSessionData BuildSession()
    {
        var loggerFactory = LoggerFactory.Create(_ => { });
        var packetSystem = new PacketSystem();
        return new CharSessionData(
            socket: null!,
            heartbeatTimeout: 30000,
            packetSystem.Factory,
            packetSystem.Registry,
            loggerFactory.CreateLogger("test"));
    }
}
