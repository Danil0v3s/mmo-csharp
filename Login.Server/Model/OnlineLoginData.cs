using Core.Timer;
using Login.Server.Repository.Impl;

namespace Login.Server.Model;

public record OnlineLoginData(
    AccountId AccountId,
    int CharServer,
    TimerId WaitingDisconnect,
    TimerId VipTimeoutTid
);