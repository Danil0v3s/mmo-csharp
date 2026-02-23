# Tools.LoginTcpClient

Small terminal client for the login socket flow.

## What it does

- Opens a TCP socket to the login server.
- Sends `CA_LOGIN` with:
  - username: `danilo`
  - password: `123456`
- Waits for first response packet.
- Parses and prints (using `PacketSystem` + packet classes):
  - `AC_ACCEPT_LOGIN (0x0AC4)`
  - `AC_REFUSE_LOGIN (0x083E)`
- After `AC_ACCEPT_LOGIN`, attempts a TCP connection to the first advertised char server.

## Run

```bash
dotnet run --project mmo-csharp/Tools.LoginTcpClient
```

Optional host/port:

```bash
dotnet run --project mmo-csharp/Tools.LoginTcpClient -- 127.0.0.1 6900
```
