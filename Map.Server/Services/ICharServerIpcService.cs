namespace Map.Server.Services;

public interface ICharServerIpcService :
    ICharServerIpcServiceCore,
    ICharServerIpcServiceInter,
    ICharServerIpcServiceParty,
    ICharServerIpcServiceGuild,
    ICharServerIpcServiceStorage,
    ICharServerIpcServiceMail,
    ICharServerIpcServiceAuction,
    ICharServerIpcServiceQuest,
    ICharServerIpcServicePet,
    ICharServerIpcServiceHomunculus,
    ICharServerIpcServiceMercenary,
    ICharServerIpcServiceElemental,
    ICharServerIpcServiceClan,
    ICharServerIpcServiceMapreg
{
}
