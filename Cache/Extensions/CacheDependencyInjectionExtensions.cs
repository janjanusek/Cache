using Cache.Base;
using Microsoft.Extensions.DependencyInjection;

namespace Cache.Extensions;

public static class CacheDependencyInjectionExtensions
{
    public static IServiceCollection RegisterCacheService<T>(this IServiceCollection serviceCollection) where T : class, ICache => serviceCollection.AddScoped<ICache, T>();
    
    public static IServiceCollection RegisterApplicationCache(this IServiceCollection serviceCollection) => serviceCollection.AddScoped<IApplicationCache, ApplicationCache>();
}