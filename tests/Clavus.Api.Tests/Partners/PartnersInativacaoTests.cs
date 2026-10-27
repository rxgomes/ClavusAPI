using System.Net;
using Clavus.Modules.Partners.Contracts;
using Clavus.Tests.Shared;

namespace Clavus.Api.Tests.Partners;

/// <summary>
/// Cenários de <c>DELETE /partners/{identifier}</c> da capability <c>partners-api</c>.
/// </summary>
[Collection(PartnersApiTestBase.Colecao)]
public sealed class PartnersInativacaoTests(PostgresFixture postgres) : PartnersApiTestBase(postgres)
{
    [Fact]
    public async Task InativarPreservaDadosAsync()
    {
        var incluido = await IncluirAsync(NovoParceiro() with
        {
            Emails = [new EmailRequest { Address = "contato@ixact.com", Type = "Comercial" }],
            Phones = [new PhoneRequest { AreaCode = "11", Number = "33334444" }],
            Contacts = [new ContactRequest { Name = "Maria Souza", Emails = [new EmailRequest { Address = "maria@ixact.com", Type = "Pessoal" }] }],
        });

        using var resposta = await Cliente.DeleteAsync($"{Rota}/{incluido.Identifier}", Cancelamento);

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        var lido = await DetalharAsync(incluido);
        Assert.False(lido.IsActive);
        Assert.Equal(["contato@ixact.com"], lido.Emails.Select(email => email.Address));
        Assert.Equal(["33334444"], lido.Phones.Select(telefone => telefone.Number));
        var contato = Assert.Single(lido.Contacts);
        Assert.Equal(["maria@ixact.com"], contato.Emails.Select(email => email.Address));
    }

    [Fact]
    public async Task InativarDeNovoRetorna204Async()
    {
        var incluido = await IncluirAsync(NovoParceiro());
        using (var primeira = await Cliente.DeleteAsync($"{Rota}/{incluido.Identifier}", Cancelamento))
        {
            Assert.Equal(HttpStatusCode.NoContent, primeira.StatusCode);
        }

        using var segunda = await Cliente.DeleteAsync($"{Rota}/{incluido.Identifier}", Cancelamento);

        Assert.Equal(HttpStatusCode.NoContent, segunda.StatusCode);
        Assert.False((await DetalharAsync(incluido)).IsActive);
    }

    [Fact]
    public async Task InexistenteRetorna404Async()
    {
        using var resposta = await Cliente.DeleteAsync($"{Rota}/{Guid.CreateVersion7()}", Cancelamento);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
    }
}
