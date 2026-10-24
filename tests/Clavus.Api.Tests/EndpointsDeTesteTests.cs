using System.Net;
using Clavus.Tests.Shared;
using Microsoft.Extensions.Hosting;

namespace Clavus.Api.Tests;

public sealed class EndpointsDeTesteTests(PostgresFixture postgres)
{
    [Fact]
    public async Task EndpointDeTesteRespondeAsync()
    {
        await using var factory = new ClavusApiFactory(Environments.Development, postgres.ConnectionString);
        using var cliente = factory.CreateClient();

        using var resposta = await cliente.GetAsync("/__testes/ok", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }
}
