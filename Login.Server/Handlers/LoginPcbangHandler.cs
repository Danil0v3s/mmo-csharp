using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CA;

namespace Login.Server.Handlers;

[PacketHandler(PacketHeader.CA_LOGIN_PCBANG)]
public class LoginPcbangHandler(LoginHandler loginHandler) : IPacketHandler<LoginSessionData, CA_LOGIN_PCBANG>
{
    public async Task HandleAsync(LoginSessionData session, CA_LOGIN_PCBANG packet)
    {
        await loginHandler.HandleLoginRequestAsync(session, packet.Username, packet.Password, packet.Clienttype, 0, false);
    }
}
