using System.Net;
using System.Net.Http.Json;
using Clavus.Modules.Partners.Contracts;
using Clavus.Tests.Shared;

namespace Clavus.Api.Tests.Partners;

/// <summary>
/// Cenários de <c>PUT /partners/{identifier}</c> da capability <c>partners-api</c>.
/// </summary>
[Collection(PartnersApiTestBase.Colecao)]
public sealed class PartnersAlteracaoTests(PostgresFixture postgres) : PartnersApiTestBase(postgres)
{
    [Fact]
    public async Task AlteracaoDeDadosAsync()
    {
        var incluido = await IncluirAsync(NovoParceiro());
        var requisicao = ParaAlteracao(incluido) with
        {
            TradeName = "Ixact Nova",
            IsSupplier = true,
            Address = new AddressRequest { ZipCode = "20040-020", City = "Rio de Janeiro", StateCode = "rj" },
        };

        var alterado = await AlterarAsync(incluido, requisicao);

        Assert.Equal("Ixact Nova", alterado.TradeName);
        Assert.True(alterado.IsSupplier);
        Assert.Equal("RJ", alterado.Address.StateCode);
        Assert.Equal("20040020", (await DetalharAsync(alterado)).Address.ZipCode);
    }

    [Fact]
    public async Task ReativarParceiroAsync()
    {
        var incluido = await IncluirAsync(NovoParceiro());
        using (var inativar = await Cliente.DeleteAsync($"{Rota}/{incluido.Identifier}", Cancelamento))
        {
            Assert.Equal(HttpStatusCode.NoContent, inativar.StatusCode);
        }

        var inativo = await DetalharAsync(incluido);
        var reativado = await AlterarAsync(inativo, ParaAlteracao(inativo) with { IsActive = true });

        Assert.True(reativado.IsActive);
        using var pesquisa = await Cliente.GetAsync($"{Rota}?isClient=true&identifier={incluido.Identifier}", Cancelamento);
        using var corpo = await LerJsonAsync(pesquisa);
        Assert.Equal(1, corpo.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task SituacaoAusenteRetorna400Async()
    {
        var incluido = await IncluirAsync(NovoParceiro());

        using var resposta = await Cliente.PutAsJsonAsync($"{Rota}/{incluido.Identifier}", ParaAlteracao(incluido) with { IsActive = null }, Cancelamento);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains("IsActive", await ChavesDeErroAsync(resposta));
    }

    [Fact]
    public async Task ParceiroInexistenteRetorna404Async()
    {
        var incluido = await IncluirAsync(NovoParceiro());

        using var resposta = await Cliente.PutAsJsonAsync($"{Rota}/{Guid.CreateVersion7()}", ParaAlteracao(incluido), Cancelamento);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task UfInvalidaRetorna400ENadaMudaAsync()
    {
        var incluido = await IncluirAsync(NovoParceiro());
        var requisicao = ParaAlteracao(incluido) with
        {
            TradeName = "Outro Nome",
            Address = new AddressRequest { ZipCode = "01310-100", StateCode = "XX" },
        };

        using var resposta = await Cliente.PutAsJsonAsync($"{Rota}/{incluido.Identifier}", requisicao, Cancelamento);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        using var corpo = await LerJsonAsync(resposta);
        Assert.Equal("UF inválida.", corpo.RootElement.GetProperty("detail").GetString());
        var lido = await DetalharAsync(incluido);
        Assert.Equal(incluido.TradeName, lido.TradeName);
        Assert.Equal(incluido.Address.StateCode, lido.Address.StateCode);
    }

    [Fact]
    public async Task DocumentoDeOutroParceiroRetorna409Async()
    {
        var outro = await IncluirAsync(NovoParceiro());
        var incluido = await IncluirAsync(NovoParceiro());

        using var resposta = await Cliente.PutAsJsonAsync(
            $"{Rota}/{incluido.Identifier}",
            ParaAlteracao(incluido) with { Document = outro.Document },
            Cancelamento);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal(incluido.Document, (await DetalharAsync(incluido)).Document);
    }

    [Fact]
    public async Task ManterOProprioDocumentoNaoEConflitoAsync()
    {
        var incluido = await IncluirAsync(NovoParceiro());

        var alterado = await AlterarAsync(incluido, ParaAlteracao(incluido));

        Assert.Equal(incluido.Document, alterado.Document);
    }

    [Fact]
    public async Task RemoverEmailEManterContatoAsync()
    {
        var incluido = await IncluirAsync(NovoParceiro() with
        {
            Emails =
            [
                new EmailRequest { Address = "comercial@ixact.com", Type = "Comercial" },
                new EmailRequest { Address = "financeiro@ixact.com", Type = "Financeiro" },
            ],
            Contacts = [new ContactRequest { Name = "Maria Souza", Role = "Compras" }],
        });
        var contato = incluido.Contacts.Single();
        var requisicao = ParaAlteracao(incluido) with
        {
            Emails = [new EmailRequest { Address = "comercial@ixact.com", Type = "Comercial" }],
            Contacts = [new ContactRequest { Identifier = contato.Identifier, Name = "Maria Souza Lima", Role = "Compras" }],
        };

        var alterado = await AlterarAsync(incluido, requisicao);

        Assert.Equal(["comercial@ixact.com"], alterado.Emails.Select(email => email.Address));
        var contatoAlterado = Assert.Single(alterado.Contacts);
        Assert.Equal(contato.Identifier, contatoAlterado.Identifier);
        Assert.Equal("Maria Souza Lima", contatoAlterado.Name);
        Assert.Equal(1, await ContarAsync("email"));
    }

    [Fact]
    public async Task IncluirContatoNovoNaAlteracaoAsync()
    {
        var incluido = await IncluirAsync(NovoParceiro() with { Contacts = [new ContactRequest { Name = "Maria Souza" }] });
        var requisicao = ParaAlteracao(incluido);
        requisicao = requisicao with { Contacts = [.. requisicao.Contacts!, new ContactRequest { Name = "João Lima" }] };

        var alterado = await AlterarAsync(incluido, requisicao);

        Assert.Equal(["Maria Souza", "João Lima"], alterado.Contacts.Select(contato => contato.Name));
        Assert.Equal(incluido.Contacts.Single().Identifier, alterado.Contacts.First().Identifier);
    }

    [Fact]
    public async Task RemoverContatoExcluiEmailsETelefonesDeleAsync()
    {
        var incluido = await IncluirAsync(NovoParceiro() with
        {
            Emails = [new EmailRequest { Address = "contato@ixact.com", Type = "Comercial" }],
            Contacts =
            [
                new ContactRequest
                {
                    Name = "Maria Souza",
                    Emails = [new EmailRequest { Address = "maria@ixact.com", Type = "Pessoal" }],
                    Phones = [new PhoneRequest { AreaCode = "11", Number = "987654321" }],
                },
            ],
        });

        var alterado = await AlterarAsync(incluido, ParaAlteracao(incluido) with { Contacts = [] });

        Assert.Empty(alterado.Contacts);
        Assert.Equal(0, await ContarAsync("contact"));
        Assert.Equal(1, await ContarAsync("email"));
        Assert.Equal(0, await ContarAsync("phone"));
    }

    [Fact]
    public async Task ContatoDeOutroParceiroRetorna400ENadaMudaAsync()
    {
        var outro = await IncluirAsync(NovoParceiro() with { Contacts = [new ContactRequest { Name = "Contato do outro" }] });
        var incluido = await IncluirAsync(NovoParceiro() with { Contacts = [new ContactRequest { Name = "Maria Souza" }] });
        var requisicao = ParaAlteracao(incluido) with
        {
            TradeName = "Outro Nome",
            Contacts = [new ContactRequest { Identifier = outro.Contacts.Single().Identifier, Name = "Invasor" }],
        };

        using var resposta = await Cliente.PutAsJsonAsync($"{Rota}/{incluido.Identifier}", requisicao, Cancelamento);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        using var corpo = await LerJsonAsync(resposta);
        Assert.Equal("Contato não encontrado neste parceiro.", corpo.RootElement.GetProperty("detail").GetString());
        var lido = await DetalharAsync(incluido);
        Assert.Equal(incluido.TradeName, lido.TradeName);
        Assert.Equal(["Maria Souza"], lido.Contacts.Select(contato => contato.Name));
        Assert.Equal(["Contato do outro"], (await DetalharAsync(outro)).Contacts.Select(contato => contato.Name));
    }

    private async Task<PartnerResponse> AlterarAsync(PartnerResponse parceiro, UpdatePartnerRequest requisicao)
    {
        using var resposta = await Cliente.PutAsJsonAsync($"{Rota}/{parceiro.Identifier}", requisicao, Cancelamento);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        return (await resposta.Content.ReadFromJsonAsync<PartnerResponse>(Cancelamento))!;
    }
}
