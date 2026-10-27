using System.Net;
using System.Net.Http.Json;
using Clavus.Modules.Partners.Contracts;
using Clavus.Tests.Shared;

namespace Clavus.Api.Tests.Partners;

/// <summary>
/// Cenários de <c>GET /partners/details</c> da capability <c>partners-api</c>.
/// </summary>
[Collection(PartnersApiTestBase.Colecao)]
public sealed class PartnersDetalheTests(PostgresFixture postgres) : PartnersApiTestBase(postgres)
{
    private const string ProblemJson = "application/problem+json";

    [Fact]
    public async Task ConsultaPorIdAsync()
    {
        var requisicao = NovoParceiro() with
        {
            Emails = [new EmailRequest { Address = "contato@ixact.com", Type = "Comercial" }],
            Contacts = [new ContactRequest { Name = "Maria Souza", Phones = [new PhoneRequest { AreaCode = "11", Number = "987654321" }] }],
        };
        var incluido = await IncluirAsync(requisicao);

        using var resposta = await Cliente.GetAsync($"{Rota}/details?isClient=true&id={incluido.Id}", Cancelamento);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var parceiro = (await resposta.Content.ReadFromJsonAsync<PartnerResponse>(Cancelamento))!;
        Assert.Equal(incluido.Identifier, parceiro.Identifier);
        Assert.Equal(incluido.Name, parceiro.Name);
        Assert.Equal(["contato@ixact.com"], parceiro.Emails.Select(email => email.Address));
        var contato = Assert.Single(parceiro.Contacts);
        Assert.Equal(incluido.Contacts.Single().Identifier, contato.Identifier);
        Assert.Equal(["987654321"], contato.Phones.Select(telefone => telefone.Number));
        Assert.NotEqual(default, parceiro.CreateIn);
        Assert.NotEqual(default, parceiro.UpdateIn);
    }

    [Fact]
    public async Task SemIdNemIdentifierRetorna400Async()
    {
        using var resposta = await Cliente.GetAsync($"{Rota}/details?isClient=true", Cancelamento);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var chaves = await ChavesDeErroAsync(resposta);
        Assert.Contains("Id", chaves);
        Assert.Contains("Identifier", chaves);
    }

    [Fact]
    public async Task SemFiltroDeTipoRetorna400Async()
    {
        var incluido = await IncluirAsync(NovoParceiro());

        using var resposta = await Cliente.GetAsync($"{Rota}/details?id={incluido.Id}", Cancelamento);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains("IsClient", await ChavesDeErroAsync(resposta));
    }

    [Fact]
    public async Task ParceiroDeOutroTipoRetorna404Async()
    {
        var cliente = await IncluirAsync(NovoParceiro(cliente: true, fornecedor: false));

        using var resposta = await Cliente.GetAsync($"{Rota}/details?isSupplier=true&id={cliente.Id}", Cancelamento);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal(ProblemJson, resposta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ParceiroInexistenteRetorna404Async()
    {
        using var resposta = await Cliente.GetAsync($"{Rota}/details?isClient=true&identifier={Guid.CreateVersion7()}", Cancelamento);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal(ProblemJson, resposta.Content.Headers.ContentType?.MediaType);
        using var corpo = await LerJsonAsync(resposta);
        Assert.Equal("Recurso não encontrado.", corpo.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ParceiroInativoEDevolvidoAsync()
    {
        var incluido = await IncluirAsync(NovoParceiro());
        using (var inativar = await Cliente.DeleteAsync($"{Rota}/{incluido.Identifier}", Cancelamento))
        {
            Assert.Equal(HttpStatusCode.NoContent, inativar.StatusCode);
        }

        var parceiro = await DetalharAsync(incluido);

        Assert.False(parceiro.IsActive);
    }
}
