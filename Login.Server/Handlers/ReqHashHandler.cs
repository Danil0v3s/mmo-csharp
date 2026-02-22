using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CA;

namespace Login.Server.Handlers;

[PacketHandler(PacketHeader.CA_REQ_HASH)]
public class ReqHashHandler(LoginHandler loginHandler) : IPacketHandler<LoginSessionData, CA_REQ_HASH>
{
    public async Task HandleAsync(LoginSessionData session, CA_REQ_HASH packet)
    {
        await loginHandler.HandleHashRequestAsync(session);
    }
}
