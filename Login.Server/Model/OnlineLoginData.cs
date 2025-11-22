namespace Login.Server.Model;

public record OnlineLoginData(
    int AccountId,
    TimerId WaitingDisconnect,
    int CharServer,
    TimerId VipTimeoutTid
);