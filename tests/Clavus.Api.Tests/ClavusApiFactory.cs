using Clavus.Shared.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clavus.Api.Tests;

/// <summary>
/// Sobe a API em memória com o ambiente, a connection string e as configurações extras informados.
/// Inclui os endpoints de <see cref="EndpointsDeTeste"/>.
/// </summary>
public sealed class ClavusApiFactory(
    string environment,
    string connectionString,
    IReadOnlyDictionary<string, string>? settings = null,
    ColetorDeLogs? logs = null) : WebApplicationFactory<Program>
{
    /// <summary>
    /// Connection string para uma porta sem servidor, para simular o banco inacessível.
    /// </summary>
    public const string BancoInacessivel = "Host=localhost;Port=1;Database=clavus;Username=clavus;Password=clavus;Timeout=2";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:Clavus", connectionString);

        foreach (var (chave, valor) in settings ?? new Dictionary<string, string>())
        {
            builder.UseSetting(chave, valor);
        }

        builder.ConfigureServices(services =>
        {
            var modulo = new ModuloDeTeste();
            services.AddSingleton<IModule>(modulo);
            modulo.RegisterServices(services, new ConfigurationBuilder().Build());
        });

        if (logs is not null)
        {
            builder.ConfigureLogging(logging => logging.AddProvider(logs));
        }
    }
}
