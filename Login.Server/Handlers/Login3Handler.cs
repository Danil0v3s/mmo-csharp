using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CA;

namespace Login.Server.Handlers;

[PacketHandler(PacketHeader.CA_LOGIN3)]
public class Login3Handler(LoginHandler loginHandler) : IPacketHandler<LoginSessionData, CA_LOGIN3>
{
    public async Task HandleAsync(LoginSessionData session, CA_LOGIN3 packet)
    {
        var passwordMd5Hex = BitConverter.ToString(packet.PasswordMD5).Replace("-", "").ToLowerInvariant();
        await loginHandler.HandleLoginRequestAsync(session, packet.Username, passwordMd5Hex, packet.Clienttype, 1, true);
    }
}
