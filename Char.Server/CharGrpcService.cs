using Core.Server.IPC;
using Grpc.Core;
using CharacterInfo = Core.Server.Packets.CharacterInfo;

namespace Char.Server;

public class CharGrpcService : CharacterService.CharacterServiceBase
{
    public override Task<CharacterListResponse> GetCharacterList(
        CharacterListRequest request, 
        ServerCallContext context)
    {
        // TODO: Query from database
        var response = new CharacterListResponse();
        response.Characters.Add(new Core.Server.IPC.CharacterInfo
        {
            CharacterId = 1001,
            Name = "Warrior123",
            Level = 50,
            ClassId = 1,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds()
        });
        response.Characters.Add(new Core.Server.IPC.CharacterInfo
        {
            CharacterId = 1002,
            Name = "Mage456",
            Level = 45,
            ClassId = 2,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-20).ToUnixTimeSeconds()
        });

        return Task.FromResult(response);
    }

    public override Task<CreateCharacterResponse> CreateCharacter(
        CreateCharacterRequest request, 
        ServerCallContext context)
    {
        // TODO: Create in database
        var response = new CreateCharacterResponse
        {
            Success = true,
            Character = new Core.Server.IPC.CharacterInfo
            {
                CharacterId = new Random().Next(10000, 99999),
                Name = request.Name,
                Level = 1,
                ClassId = request.ClassId,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            }
        };

        return Task.FromResult(response);
    }

    public override Task<DeleteCharacterResponse> DeleteCharacter(
        DeleteCharacterRequest request, 
        ServerCallContext context)
    {
        // TODO: Delete from database
        var response = new DeleteCharacterResponse
        {
            Success = true
        };

        return Task.FromResult(response);
    }

    public override Task<CharacterDataResponse> GetCharacterData(
        CharacterDataRequest request, 
        ServerCallContext context)
    {
        // TODO: Query from database
        var response = new CharacterDataResponse
        {
            Character = new Core.Server.IPC.CharacterInfo
            {
                CharacterId = request.CharacterId,
                Name = "TestChar",
                Level = 50,
                ClassId = 1,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds()
            },
            MapId = 1,
            PositionX = 100.0f,
            PositionY = 0.0f,
            PositionZ = 100.0f
        };

        return Task.FromResult(response);
    }
}

