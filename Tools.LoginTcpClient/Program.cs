using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Buffers.Binary;
using Core.Server.Packets;
using InAcAcceptLogin = Core.Server.Packets.In.AC.AC_ACCEPT_LOGIN;
using InAcRefuseLogin = Core.Server.Packets.In.AC.AC_REFUSE_LOGIN;

string host = args.Length > 0 ? args[0] : "127.0.0.1";
int port = args.Length > 1 && int.TryParse(args[1], out var parsedPort) ? parsedPort : 6900;

const string username = "danilo";
const string password = "123456";
const uint version = 1;
const byte clientType = 0;

Console.WriteLine("[1/5] Creating TCP socket...");
using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

try
{
    Console.WriteLine($"[2/5] Connecting to {host}:{port}...");
    await socket.ConnectAsync(host, port);
    Console.WriteLine("Connected.");

    Console.WriteLine("[3/5] Building CA_LOGIN packet...");
    byte[] loginPacket = BuildCaLoginPacket(username, password, version, clientType);
    Console.WriteLine($"CA_LOGIN bytes: {loginPacket.Length} (header=0x{(short)PacketHeader.CA_LOGIN:X4})");

    Console.WriteLine("[4/5] Sending CA_LOGIN...");
    await socket.SendAsync(loginPacket, SocketFlags.None);
    Console.WriteLine("CA_LOGIN sent.");

    Console.WriteLine("[5/5] Waiting for server response...");
    byte[] headerBytes = await ReadExactAsync(socket, 2);
    short headerValue = BitConverter.ToInt16(headerBytes, 0);
    byte[] packetBytes = await ReadRemainingPacketBytesAsync(socket, headerBytes, headerValue);

    var packetSystem = new PacketSystem();
    using var packetStream = new MemoryStream(packetBytes);
    using var packetReader = new BinaryReader(packetStream);
    var packet = packetSystem.ReadPacket(packetReader);

    switch (packet)
    {
        case InAcAcceptLogin accept:
            await HandleAcAcceptLoginAsync(accept, host);
            break;
        case InAcRefuseLogin refuse:
            HandleAcRefuseLogin(refuse);
            break;
        default:
            Console.WriteLine($"Unexpected/unsupported packet header: 0x{headerValue:X4}");
            break;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Socket flow failed: {ex.GetType().Name}: {ex.Message}");
}

static async Task HandleAcAcceptLoginAsync(InAcAcceptLogin packet, string loginHost)
{
    Console.WriteLine("Received AC_ACCEPT_LOGIN (parsed by packet class)");
    Console.WriteLine($"Header: 0x{(short)PacketHeader.AC_ACCEPT_LOGIN:X4}");
    Console.WriteLine($"PacketLength: {packet.PacketLength}");
    Console.WriteLine($"AID: {packet.AID}");
    Console.WriteLine($"LoginId1: {packet.LoginId1}");
    Console.WriteLine($"LoginId2: {packet.LoginId2}");
    Console.WriteLine($"LastIp(raw uint): {packet.LastIp}");
    Console.WriteLine($"LastLogin: {packet.LastLogin}");
    Console.WriteLine($"Sex: {(char)packet.Sex} ({packet.Sex})");
    Console.WriteLine($"Token: {packet.Token}");
    Console.WriteLine($"CharServers: {packet.CharServers.Length}");

    for (int i = 0; i < packet.CharServers.Length; i++)
    {
        var server = packet.CharServers[i];
        string ipString = ConvertPackedIpToAddress(server.Ip).ToString();

        Console.WriteLine(
            $"  [{i}] Name={server.Name}, Ip={ipString} (raw={server.Ip}), Port={server.Port}, Users={server.Users}, Type={server.Type}, New={server.New}, UnknownBytes={server.Unknown.Length}");
    }

    await TryConnectFirstCharServerAsync(packet, loginHost);
    Console.WriteLine("Login flow step complete: AC_ACCEPT_LOGIN parsed.");
}

static void HandleAcRefuseLogin(InAcRefuseLogin packet)
{
    Console.WriteLine("Received AC_REFUSE_LOGIN (parsed by packet class)");
    Console.WriteLine($"Header: 0x{(short)PacketHeader.AC_REFUSE_LOGIN:X4}");
    Console.WriteLine($"ErrorCode: {packet.Error}");
    Console.WriteLine($"UnblockTime: {packet.UnblockTime}");
    Console.WriteLine("Credentials were rejected by the login server.");
}

static byte[] BuildCaLoginPacket(string username, string password, uint version, byte clientType)
{
    using var ms = new MemoryStream();
    using var writer = new BinaryWriter(ms);

    writer.Write((short)PacketHeader.CA_LOGIN);
    writer.Write(version);
    WriteFixedString(writer, username, 24);
    WriteFixedString(writer, password, 24);
    writer.Write(clientType);

    return ms.ToArray();
}

static async Task<byte[]> ReadRemainingPacketBytesAsync(Socket socket, byte[] headerBytes, short headerValue)
{
    var header = (PacketHeader)headerValue;
    byte[] remainingBytes;

    if (header == PacketHeader.AC_ACCEPT_LOGIN)
    {
        byte[] sizeBytes = await ReadExactAsync(socket, 2);
        short packetLength = BitConverter.ToInt16(sizeBytes, 0);
        if (packetLength < 4)
        {
            throw new InvalidDataException($"Invalid variable packet length: {packetLength}");
        }

        byte[] bodyBytes = await ReadExactAsync(socket, packetLength - 4);
        remainingBytes = new byte[sizeBytes.Length + bodyBytes.Length];
        Buffer.BlockCopy(sizeBytes, 0, remainingBytes, 0, sizeBytes.Length);
        Buffer.BlockCopy(bodyBytes, 0, remainingBytes, sizeBytes.Length, bodyBytes.Length);
    }
    else if (header == PacketHeader.AC_REFUSE_LOGIN)
    {
        const int fixedBodySize = 24;
        remainingBytes = await ReadExactAsync(socket, fixedBodySize);
    }
    else
    {
        throw new InvalidDataException($"Unsupported response packet header: 0x{headerValue:X4}");
    }

    byte[] fullPacket = new byte[headerBytes.Length + remainingBytes.Length];
    Buffer.BlockCopy(headerBytes, 0, fullPacket, 0, headerBytes.Length);
    Buffer.BlockCopy(remainingBytes, 0, fullPacket, headerBytes.Length, remainingBytes.Length);
    return fullPacket;
}

static void WriteFixedString(BinaryWriter writer, string value, int length)
{
    byte[] bytes = Encoding.UTF8.GetBytes(value);
    int bytesToWrite = Math.Min(bytes.Length, length);

    writer.Write(bytes, 0, bytesToWrite);

    int remaining = length - bytesToWrite;
    if (remaining > 0)
    {
        writer.Write(new byte[remaining]);
    }
}

static async Task<byte[]> ReadExactAsync(Socket socket, int length)
{
    byte[] buffer = new byte[length];
    int read = 0;

    while (read < length)
    {
        int received = await socket.ReceiveAsync(buffer.AsMemory(read, length - read), SocketFlags.None);
        if (received <= 0)
        {
            throw new IOException($"Socket closed while reading {length} bytes (only {read} read).");
        }

        read += received;
    }

    return buffer;
}

static async Task TryConnectFirstCharServerAsync(InAcAcceptLogin packet, string loginHost)
{
    if (packet.CharServers.Length == 0)
    {
        Console.WriteLine("No char servers advertised by login server.");
        return;
    }

    var first = packet.CharServers[0];
    IPAddress targetIp = ConvertPackedIpToAddress(first.Ip);
    int targetPort = first.Port;

    // Some environments advertise 0.0.0.0 to mean "same host as login server".
    if (targetIp.Equals(IPAddress.Any) || targetIp.Equals(IPAddress.IPv6Any))
    {
        Console.WriteLine("First char server IP is 0.0.0.0; using login host as fallback.");
        if (IPAddress.TryParse(loginHost, out var parsedIp))
        {
            targetIp = parsedIp;
        }
        else
        {
            var resolved = await Dns.GetHostAddressesAsync(loginHost);
            var firstV4 = resolved.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            if (firstV4 == null)
            {
                Console.WriteLine("Could not resolve an IPv4 address for login host; skipping char server connect.");
                return;
            }

            targetIp = firstV4;
        }
    }

    Console.WriteLine($"Attempting char server connect: {targetIp}:{targetPort} (server: {first.Name})");

    using var charSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    try
    {
        await charSocket.ConnectAsync(targetIp, targetPort, cts.Token);
        Console.WriteLine("Char server connect: SUCCESS");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Char server connect: FAILED ({ex.GetType().Name}: {ex.Message})");
    }
}

static IPAddress ConvertPackedIpToAddress(uint packedIp)
{
    uint hostOrder = BitConverter.IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(packedIp)
        : packedIp;
    return new IPAddress(hostOrder);
}
