using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Clavus.Api.Tests;

/// <summary>
/// Sobe a API em memória com o ambiente e a connection string informados.
/// </summary>
public sealed class ClavusApiFactory(string environment, string connectionString) : WebApplicationFactory<Program>
{
    /// <summary>
    /// Connection string para uma porta sem servidor, para simular o banco inacessível.
    /// </summary>
    public const string BancoInacessivel = "Host=localhost;Port=1;Database=clavus;Username=postgres;Password=ixact;Timeout=2";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:Clavus", connectionString);
    }
}
