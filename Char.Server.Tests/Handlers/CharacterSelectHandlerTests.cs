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

}
