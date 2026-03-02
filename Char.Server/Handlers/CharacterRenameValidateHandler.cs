using Core.Database.Repositories.Api;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Core.Server.Packets.Out.HC;
using Char.Server.Services;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_REQ_IS_VALID_CHARNAME)]
public class CharacterRenameValidateHandler(
    ICharacterRepository characterRepository,
    CharServerConfiguration configuration
) : IPacketHandler<CharSessionData, CH_REQ_IS_VALID_CHARNAME>
{
    public async Task HandleAsync(CharSessionData session, CH_REQ_IS_VALID_CHARNAME packet)
    {
        if (packet.AccountId != (uint)session.AccountId.GetValueOrDefault())
        {
            session.Disconnect(DisconnectReason.Kicked);
            return;
        }

        var character = await characterRepository.GetByIdAsync((int)packet.CharId);
        if (character is null || character.AccountId != session.AccountId.Value)
        {
            return;
        }

        var name = CharacterNamePolicy.Normalize(packet.NewName);
        var isValid = await IsRenameCandidateValidAsync(name);
        if (isValid)
        {
            session.PendingCharacterRename = name;
        }

        session.EnqueuePacket(new HC_ACK_IS_VALID_CHARNAME
        {
            Result = (ushort)(isValid ? 1 : 0)
        });
    }

    private async Task<bool> IsRenameCandidateValidAsync(string name)
    {
        if (!CharacterNamePolicy.IsStructurallyValid(name, configuration))
        {
            return false;
        }
        return !await CharacterNamePolicy.NameExistsAsync(characterRepository, name, configuration);
    }
}
