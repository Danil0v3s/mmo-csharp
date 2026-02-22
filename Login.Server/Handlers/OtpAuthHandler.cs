using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;

namespace Login.Server.Handlers;

[PacketHandler(PacketHeader.CT_AUTH)]
public class OtpAuthHandler(LoginHandler loginHandler) : IPacketHandler<LoginSessionData, CT_AUTH>
{
    public async Task HandleAsync(LoginSessionData session, CT_AUTH packet)
    {
        await loginHandler.HandleOtpAuthAsync(session);
    }
}
