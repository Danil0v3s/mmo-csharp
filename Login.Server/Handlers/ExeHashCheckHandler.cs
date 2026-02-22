using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CA;

namespace Login.Server.Handlers;

[PacketHandler(PacketHeader.CA_EXE_HASHCHECK)]
public class ExeHashCheckHandler(LoginHandler loginHandler) : IPacketHandler<LoginSessionData, CA_EXE_HASHCHECK>
{
    public async Task HandleAsync(LoginSessionData session, CA_EXE_HASHCHECK packet)
    {
        await loginHandler.HandleClientHashAsync(session, packet.Hash);
    }
}
