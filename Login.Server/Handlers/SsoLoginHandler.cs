using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CA;
using Login.Server.UseCase;

namespace Login.Server.Handlers;

[PacketHandler(PacketHeader.CA_SSO_LOGIN_REQ)]
public class SsoLoginHandler(ILoginAuthUseCase loginAuthUseCase) : IPacketHandler<LoginSessionData, CA_SSO_LOGIN_REQ>
{
    public async Task HandleAsync(LoginSessionData session, CA_SSO_LOGIN_REQ packet)
    {
        // For now, keep parity with rAthena behavior where SSO token is used as password.
        await loginAuthUseCase.HandleLoginRequestAsync(session, packet.Username, packet.Token, packet.Clienttype, 0, false);
    }
}
