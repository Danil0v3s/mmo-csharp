namespace Login.Server;

/// <summary>
/// Represents character server data, equivalent to C++ mmo_char_server struct
/// </summary>
public record CharServerData
{
    public string Name { get; init; } = string.Empty;  // char-serv name
    public int SocketFd { get; init; } = -1;           // char-serv socket (file descriptor)
    public uint Ip { get; init; } = 0;                 // char-serv IP
    public ushort Port { get; init; } = 0;             // char-serv port
    public ushort Users { get; init; } = 0;            // user count on this server
    public ushort Type { get; init; } = 0;             // 0=normal, 1=maintenance, 2=over 18, 3=paying, 4=P2P
    public ushort New { get; init; } = 0;              // should display as 'new'?
}