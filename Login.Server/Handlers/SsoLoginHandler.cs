using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CA;

namespace Login.Server.Handlers;

[PacketHandler(PacketHeader.CA_SSO_LOGIN_REQ)]
public class SsoLoginHandler(LoginHandler loginHandler) : IPacketHandler<LoginSessionData, CA_SSO_LOGIN_REQ>
{
    public async Task HandleAsync(LoginSessionData session, CA_SSO_LOGIN_REQ packet)
    {
        // For now, keep parity with rAthena behavior where SSO token is used as password.
        await loginHandler.HandleLoginRequestAsync(session, packet.Username, packet.Token, packet.Clienttype, 0, false);
    }
}
