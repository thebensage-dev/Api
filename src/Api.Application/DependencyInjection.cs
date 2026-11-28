using Api.Application.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.Application;

public static class DependencyInjection
{
    /// <summary>The services of the use cases. Their repositories come from <c>AddInfrastructure</c>.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
