using Microsoft.Extensions.DependencyInjection;

namespace MikaServerCore.Service;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMikaNetwork(this IServiceCollection services)
    {
        return services;
    }
}