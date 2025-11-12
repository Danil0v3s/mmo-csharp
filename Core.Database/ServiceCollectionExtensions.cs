using Core.Database.Context;
using Core.Database.Repositories;
using Core.Database.Repositories.Api;
using Core.Database.Repositories.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Database;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameDatabase(
        this IServiceCollection services,
        string connectionString)
    {
        // Register DbContext (acts as built-in Unit of Work)
        services.AddDbContext<GameDbContext>(options =>
        {
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                mySqlOptions =>
                {
                    mySqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                });
        });

        // Register Repositories - inject these directly into your services
        services.AddScoped<ILoginRepository, LoginRepository>();
        services.AddScoped<ICharacterRepository, CharacterRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ICartInventoryRepository, CartInventoryRepository>();
        services.AddScoped<IStorageRepository, StorageRepository>();
        services.AddScoped<IGuildStorageRepository, GuildStorageRepository>();
        services.AddScoped<IGuildRepository, GuildRepository>();
        services.AddScoped<IGuildMemberRepository, GuildMemberRepository>();
        services.AddScoped<IGuildAllianceRepository, GuildAllianceRepository>();
        services.AddScoped<IClanRepository, ClanRepository>();
        services.AddScoped<IPartyRepository, PartyRepository>();
        services.AddScoped<IFriendRepository, FriendRepository>();
        services.AddScoped<IMailRepository, MailRepository>();
        services.AddScoped<IPetRepository, PetRepository>();
        services.AddScoped<IHomunculusRepository, HomunculusRepository>();
        services.AddScoped<IElementalRepository, ElementalRepository>();
        services.AddScoped<IMercenaryRepository, MercenaryRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<IQuestRepository, QuestRepository>();
        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<IAuctionRepository, AuctionRepository>();
        services.AddScoped<IVendingRepository, VendingRepository>();
        services.AddScoped<IBuyingStoreRepository, BuyingStoreRepository>();
        services.AddScoped<IMarketRepository, MarketRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IMobRepository, MobRepository>();

        return services;
    }
}

