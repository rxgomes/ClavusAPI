using System.Net;
using System.Net.Http.Json;
using Clavus.Modules.Partners.Contracts;
using Clavus.Modules.Partners.Domain;
using Clavus.Tests.Shared;

namespace Clavus.Api.Tests.Partners;

/// <summary>
/// Cenários de <c>POST /partners</c> da capability <c>partners-api</c>.
/// </summary>
[Collection(PartnersApiTestBase.Colecao)]
public sealed class PartnersInclusaoTests(PostgresFixture postgres) : PartnersApiTestBase(postgres)
{
    [Fact]
    public async Task InclusaoMinimaRetorna201Async()
    {
        var parceiro = await IncluirAsync(NovoParceiro());

        Assert.True(parceiro.Id > 0);
        Assert.NotEqual(Guid.Empty, parceiro.Identifier);
    }

    [Fact]
    public async Task InclusaoCompletaAsync()
    {
        var requisicao = NovoParceiro(documento: "529.982.247-25") with
        {
            Emails = [new EmailRequest { Address = "contato@ixact.com", Type = "Comercial" }],
            Phones = [new PhoneRequest { AreaCode = "11", Number = "33334444" }],
            Contacts =
            [
                new ContactRequest
                {
                    Name = "Maria Souza",
                    Role = "Compras",
                    Emails = [new EmailRequest { Address = "maria@ixact.com", Type = "Pessoal" }],
                },
            ],
        };

        var parceiro = await IncluirAsync(requisicao);

        Assert.Equal("52998224725", parceiro.Document);
        Assert.Equal(DocumentType.Individual, parceiro.DocumentType);
        Assert.True(parceiro.IsActive);
        Assert.True(parceiro.IsClient);
        Assert.Equal(["contato@ixact.com"], parceiro.Emails.Select(email => email.Address));
        Assert.Equal(["33334444"], parceiro.Phones.Select(telefone => telefone.Number));
        var contato = Assert.Single(parceiro.Contacts);
        Assert.Equal("Maria Souza", contato.Name);
        Assert.Equal(["maria@ixact.com"], contato.Emails.Select(email => email.Address));
    }

    [Fact]
    public async Task NormalizacaoAplicadaAsync()
    {
        var requisicao = NovoParceiro() with
        {
            Address = new AddressRequest { ZipCode = "01310-100", StateCode = "sp" },
            Phones = [new PhoneRequest { AreaCode = "(11)", Number = "98765-4321" }],
        };

        var parceiro = await IncluirAsync(requisicao);

        Assert.Equal("SP", parceiro.Address.StateCode);
        Assert.Equal("01310100", parceiro.Address.ZipCode);
        var telefone = Assert.Single(parceiro.Phones);
        Assert.Equal("11", telefone.AreaCode);
        Assert.Equal("987654321", telefone.Number);
    }

    [Fact]
    public async Task NenhumIndicadorRetorna400Async()
    {
        using var resposta = await Cliente.PostAsJsonAsync(Rota, NovoParceiro(cliente: false, fornecedor: false), Cancelamento);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var chaves = await ChavesDeErroAsync(resposta);
        Assert.Contains("IsClient", chaves);
        Assert.Contains("IsSupplier", chaves);
        Assert.Equal(0, await ContarAsync("partner"));
    }

    [Fact]
    public async Task ClienteEFornecedorAsync()
    {
        var parceiro = await IncluirAsync(NovoParceiro(cliente: true, fornecedor: true));

        Assert.True(parceiro.IsClient);
        Assert.True(parceiro.IsSupplier);
    }

    [Fact]
    public async Task NomeFantasiaAusenteRetorna400Async()
    {
        using var resposta = await Cliente.PostAsJsonAsync(Rota, NovoParceiro() with { TradeName = null }, Cancelamento);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        using var corpo = await LerJsonAsync(resposta);
        Assert.Equal("Um ou mais campos são inválidos.", corpo.RootElement.GetProperty("title").GetString());
        var mensagens = corpo.RootElement.GetProperty("errors").GetProperty("TradeName").EnumerateArray().Select(m => m.GetString());
        Assert.Equal(["O nome fantasia é obrigatório."], mensagens);
    }

    [Fact]
    public async Task EmailDeContatoSemTipoRetorna400Async()
    {
        var requisicao = NovoParceiro() with
        {
            Contacts =
            [
                new ContactRequest
                {
                    Name = "Maria Souza",
                    Emails = [new EmailRequest { Address = "maria@ixact.com" }],
                },
            ],
        };

        using var resposta = await Cliente.PostAsJsonAsync(Rota, requisicao, Cancelamento);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var chaves = await ChavesDeErroAsync(resposta);
        Assert.Contains(chaves, chave => chave.Contains("Contacts[0]") && chave.Contains("Emails[0]") && chave.EndsWith("Type"));
    }

    [Fact]
    public async Task CpfInvalidoRetorna400ComDetailAsync()
    {
        using var resposta = await Cliente.PostAsJsonAsync(Rota, NovoParceiro(documento: "52998224726"), Cancelamento);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        using var corpo = await LerJsonAsync(resposta);
        Assert.Equal("CPF inválido.", corpo.RootElement.GetProperty("detail").GetString());
        Assert.Equal(0, await ContarAsync("partner"));
    }

    [Fact]
    public async Task DocumentoDuplicadoFormatadoDeOutroJeitoRetorna409Async()
    {
        await IncluirAsync(NovoParceiro(documento: "11222333000181"));

        using var resposta = await Cliente.PostAsJsonAsync(Rota, NovoParceiro(documento: "11.222.333/0001-81"), Cancelamento);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        using var corpo = await LerJsonAsync(resposta);
        Assert.Equal("Conflito.", corpo.RootElement.GetProperty("title").GetString());
        Assert.Equal("Já existe um parceiro com o documento informado.", corpo.RootElement.GetProperty("detail").GetString());
        Assert.Equal(1, await ContarAsync("partner"));
    }
}
