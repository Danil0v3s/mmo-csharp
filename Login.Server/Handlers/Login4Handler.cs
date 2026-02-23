using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CA;
using Login.Server.UseCase;

namespace Login.Server.Handlers;

[PacketHandler(PacketHeader.CA_LOGIN4)]
public class Login4Handler(ILoginAuthUseCase loginAuthUseCase) : IPacketHandler<LoginSessionData, CA_LOGIN4>
{
    public async Task HandleAsync(LoginSessionData session, CA_LOGIN4 packet)
    {
        var passwordMd5Hex = BitConverter.ToString(packet.PasswordMD5).Replace("-", "").ToLowerInvariant();
        await loginAuthUseCase.HandleLoginRequestAsync(session, packet.Username, passwordMd5Hex, packet.Clienttype, 1, true);
    }
}
