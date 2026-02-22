using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CA;

namespace Login.Server.Handlers;

[PacketHandler(PacketHeader.CA_CONNECT_INFO_CHANGED)]
public class LoginKeepAliveHandler : IPacketHandler<LoginSessionData, CA_CONNECT_INFO_CHANGED>
{
    public Task HandleAsync(LoginSessionData session, CA_CONNECT_INFO_CHANGED packet)
    {
        return Task.CompletedTask;
    }
}
