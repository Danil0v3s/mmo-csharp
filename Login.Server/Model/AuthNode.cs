namespace Login.Server.Model;

public record AuthNode(
    int AccountId,
    int LoginId1,
    int LoginId2,
    int Ip,
    char Sex,
    byte ClientType
);