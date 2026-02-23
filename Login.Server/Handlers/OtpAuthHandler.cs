using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.ServerPackets;
using Login.Server.UseCase;

namespace Login.Server.Handlers;

[PacketHandler(PacketHeader.CT_AUTH)]
public class OtpAuthHandler : IPacketHandler<LoginSessionData, CT_AUTH>
{
    public Task HandleAsync(LoginSessionData session, CT_AUTH packet)
    {
        var res = new TC_RESULT
        {
            PacketLength = (short)(2 + 2 + 4 + 20 + 6),
            Type = 0,
            Unknown1 = "S1000",
            Unknown2 = "token"
        };

        session.EnqueuePacket(res);
        return Task.CompletedTask;
    }
}
