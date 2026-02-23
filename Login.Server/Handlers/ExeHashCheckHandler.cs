using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CA;
using Login.Server.UseCase;

namespace Login.Server.Handlers;

[PacketHandler(PacketHeader.CA_EXE_HASHCHECK)]
public class ExeHashCheckHandler : IPacketHandler<LoginSessionData, CA_EXE_HASHCHECK>
{
    public Task HandleAsync(LoginSessionData session, CA_EXE_HASHCHECK packet)
    {
        session.HasClientHash = 1;
        session.ClientHash = packet.Hash;
        return Task.CompletedTask;
    }
}
