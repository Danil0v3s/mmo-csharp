using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CA;
using Login.Server.UseCase;

namespace Login.Server.Handlers;

[PacketHandler(PacketHeader.CA_LOGIN)]
public class LoginHandler(ILoginAuthUseCase loginAuthUseCase) : IPacketHandler<LoginSessionData, CA_LOGIN>
{
    public async Task HandleAsync(LoginSessionData session, CA_LOGIN packet)
    {
        await loginAuthUseCase.HandleLoginRequestAsync(
            session,
            packet.Username,
            packet.Password,
            packet.Clienttype,
            0,
            false);
    }
}
