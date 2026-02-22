using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CA;

namespace Login.Server.Handlers;

[PacketHandler(PacketHeader.CA_LOGIN_CHANNEL)]
public class LoginChannelHandler(LoginHandler loginHandler) : IPacketHandler<LoginSessionData, CA_LOGIN_CHANNEL>
{
    public async Task HandleAsync(LoginSessionData session, CA_LOGIN_CHANNEL packet)
    {
        await loginHandler.HandleLoginRequestAsync(session, packet.Username, packet.Password, packet.Clienttype, 0, false);
    }
}
