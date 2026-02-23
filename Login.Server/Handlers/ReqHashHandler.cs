using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CA;
using Core.Server.Packets.Out.AC;
using Login.Server.UseCase;

namespace Login.Server.Handlers;

[PacketHandler(PacketHeader.CA_REQ_HASH)]
public class ReqHashHandler : IPacketHandler<LoginSessionData, CA_REQ_HASH>
{
    public Task HandleAsync(LoginSessionData session, CA_REQ_HASH packet)
    {
        session.Md5Key = GenerateMd5Salt(16);
        var res = new AC_ACK_HASH
        {
            PacketLength = (short)(4 + session.Md5Key.Length),
            Salt = session.Md5Key
        };

        session.EnqueuePacket(res);
        return Task.CompletedTask;
    }
    
    private static string GenerateMd5Salt(int length)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        Span<byte> randomBytes = stackalloc byte[length];
        System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);
        var output = new char[length];

        for (var i = 0; i < length; i++)
        {
            output[i] = alphabet[randomBytes[i] % alphabet.Length];
        }

        return new string(output);
    }
}
