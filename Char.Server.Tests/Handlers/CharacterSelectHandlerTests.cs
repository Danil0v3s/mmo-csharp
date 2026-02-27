using Char.Server.Handlers;
using Core.Database.Entities;

namespace Char.Server.Tests.Handlers;

public class CharacterSelectHandlerTests
{
    [Fact]
    public void TrySelectCharacterForSlot_ShouldReturnMatchingNonDeletedCharacter()
    {
        var characters = new List<CharEntity>
        {
            new() { CharId = 1001, AccountId = 2000000, CharNum = 0, DeleteDate = 0 },
            new() { CharId = 1002, AccountId = 2000000, CharNum = 1, DeleteDate = 0 }
        };

        var result = CharacterSelectHandler.TrySelectCharacterForSlot(
            characters,
            accountId: 2000000,
            slot: 1,
            out var selected);

        Assert.True(result);
        Assert.Equal(1002, selected.CharId);
    }

    [Fact]
    public void TrySelectCharacterForSlot_ShouldRejectDeletedOrWrongAccountCharacter()
    {
        var characters = new List<CharEntity>
        {
            new() { CharId = 1001, AccountId = 2000000, CharNum = 0, DeleteDate = 1 },
            new() { CharId = 1002, AccountId = 2000001, CharNum = 0, DeleteDate = 0 }
        };

        var result = CharacterSelectHandler.TrySelectCharacterForSlot(
            characters,
            accountId: 2000000,
            slot: 0,
            out _);

        Assert.False(result);
    }

    [Fact]
    public void ResolveDestinationMapName_ShouldPreferLastMapThenSaveMapThenStartPoint()
    {
        var configuration = new CharServerConfiguration
        {
            StartPoint =
            [
                new StartPoint { Map = "new_1-1", X = 53, Y = 111 }
            ]
        };

        var withLastMap = new CharEntity { LastMap = "prt_fild08", SaveMap = "prontera" };
        Assert.Equal("prt_fild08", CharacterSelectHandler.ResolveDestinationMapName(withLastMap, configuration));

        var withSaveMap = new CharEntity { LastMap = "", SaveMap = "geffen" };
        Assert.Equal("geffen", CharacterSelectHandler.ResolveDestinationMapName(withSaveMap, configuration));

        var empty = new CharEntity { LastMap = "", SaveMap = "" };
        Assert.Equal("new_1-1", CharacterSelectHandler.ResolveDestinationMapName(empty, configuration));
    }

    [Fact]
    public void TryResolveMapEndpoint_ShouldPreferConfiguredMapIpAndPort()
    {
        var configuration = new CharServerConfiguration
        {
            MapIp = "192.168.10.15",
            MapPort = 5200
        };

        var result = CharacterSelectHandler.TryResolveMapEndpoint(configuration, out var ip, out var port);

        Assert.True(result);
        Assert.Equal((uint)0xC0A80A0F, ip);
        Assert.Equal((ushort)5200, port);
    }
}
