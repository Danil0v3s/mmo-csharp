using Char.Server.Services;

namespace Char.Server.Tests.Services;

public class CharacterListFlowServiceGateTests
{
    [Fact]
    public void IsBlockedByMaintenanceOrCapacity_ShouldBlock_WhenMaintenanceAndNonGm()
    {
        var blocked = CharacterListFlowService.IsBlockedByMaintenanceOrCapacity(
            charMaintenance: 1,
            maxConnectUser: -1,
            connectedUsers: 10,
            groupId: 0,
            gmAllowGroup: 99);

        Assert.True(blocked);
    }

    [Fact]
    public void IsBlockedByMaintenanceOrCapacity_ShouldAllow_WhenMaintenanceAndGmBypass()
    {
        var blocked = CharacterListFlowService.IsBlockedByMaintenanceOrCapacity(
            charMaintenance: 1,
            maxConnectUser: -1,
            connectedUsers: 10,
            groupId: 99,
            gmAllowGroup: 99);

        Assert.False(blocked);
    }

    [Fact]
    public void IsBlockedByMaintenanceOrCapacity_ShouldBlock_WhenMaxConnectUserIsZeroAndNonGm()
    {
        var blocked = CharacterListFlowService.IsBlockedByMaintenanceOrCapacity(
            charMaintenance: 0,
            maxConnectUser: 0,
            connectedUsers: 0,
            groupId: 10,
            gmAllowGroup: 99);

        Assert.True(blocked);
    }

    [Fact]
    public void IsBlockedByMaintenanceOrCapacity_ShouldBlock_WhenAtCapacityAndNonGm()
    {
        var blocked = CharacterListFlowService.IsBlockedByMaintenanceOrCapacity(
            charMaintenance: 0,
            maxConnectUser: 3,
            connectedUsers: 3,
            groupId: 10,
            gmAllowGroup: 99);

        Assert.True(blocked);
    }

    [Fact]
    public void IsBlockedByMaintenanceOrCapacity_ShouldAllow_WhenAtCapacityButGmBypass()
    {
        var blocked = CharacterListFlowService.IsBlockedByMaintenanceOrCapacity(
            charMaintenance: 0,
            maxConnectUser: 3,
            connectedUsers: 10,
            groupId: 99,
            gmAllowGroup: 99);

        Assert.False(blocked);
    }

    [Fact]
    public void IsBlockedByMaintenanceOrCapacity_ShouldAllow_WhenUnderCapacityAndNoMaintenance()
    {
        var blocked = CharacterListFlowService.IsBlockedByMaintenanceOrCapacity(
            charMaintenance: 0,
            maxConnectUser: 3,
            connectedUsers: 2,
            groupId: 1,
            gmAllowGroup: 99);

        Assert.False(blocked);
    }
}
