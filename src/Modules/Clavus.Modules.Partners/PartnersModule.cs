using Clavus.Modules.Partners.Persistence;
using Clavus.Shared.Abstractions;
using Clavus.Shared.Infrastructure.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clavus.Modules.Partners;

/// <summary>
/// Partners module: business partners (clients and suppliers)
/// </summary>
public sealed class PartnersModule : IModule
{
    /// <inheritdoc />
    public string Name => "Partners";

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<PartnersDbContext>(PartnersDbContext.SchemaName);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
