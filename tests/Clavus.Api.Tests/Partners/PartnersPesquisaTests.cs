using System.Net;
using System.Net.Http.Json;
using Clavus.Modules.Partners.Contracts;
using Clavus.Shared.Abstractions.Pagination;
using Clavus.Tests.Shared;

namespace Clavus.Api.Tests.Partners;

/// <summary>
/// Cenários de <c>GET /partners</c> da capability <c>partners-api</c>.
/// </summary>
[Collection(PartnersApiTestBase.Colecao)]
public sealed class PartnersPesquisaTests(PostgresFixture postgres) : PartnersApiTestBase(postgres)
{
    [Fact]
    public async Task PesquisaDeClientesTrazSoClientesAsync()
    {
        var soCliente = await IncluirAsync(NovoParceiro(cliente: true, fornecedor: false));
        var soFornecedor = await IncluirAsync(NovoParceiro(cliente: false, fornecedor: true));
        var ambos = await IncluirAsync(NovoParceiro(cliente: true, fornecedor: true));

        var pagina = await PesquisarAsync("isClient=true");

        var identifiers = pagina.Items.Select(item => item.Identifier).ToList();
        Assert.Contains(soCliente.Identifier, identifiers);
        Assert.Contains(ambos.Identifier, identifiers);
        Assert.DoesNotContain(soFornecedor.Identifier, identifiers);
    }

    [Theory]
    [InlineData("isClient=true&isSupplier=true")]
    [InlineData("")]
    [InlineData("isClient=false")]
    [InlineData("isSupplier=false")]
    [InlineData("isClient=false&isSupplier=true")]
    public async Task FiltroDeTipoInvalidoRetorna400Async(string filtro)
    {
        using var resposta = await Cliente.GetAsync($"{Rota}?{filtro}", Cancelamento);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var chaves = await ChavesDeErroAsync(resposta);
        Assert.Contains("IsClient", chaves);
        Assert.Contains("IsSupplier", chaves);
    }

    [Fact]
    public async Task IdEIdentifierJuntosRetorna400Async()
    {
        using var resposta = await Cliente.GetAsync($"{Rota}?isClient=true&id=1&identifier=0199f5a2-0000-7000-8000-000000000000", Cancelamento);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var chaves = await ChavesDeErroAsync(resposta);
        Assert.Contains("Id", chaves);
        Assert.Contains("Identifier", chaves);
    }

    [Fact]
    public async Task PesquisaPorIdentifierAsync()
    {
        var fornecedor = await IncluirAsync(NovoParceiro(cliente: false, fornecedor: true));
        await IncluirAsync(NovoParceiro(cliente: false, fornecedor: true));

        var pagina = await PesquisarAsync($"isSupplier=true&identifier={fornecedor.Identifier}");

        var item = Assert.Single(pagina.Items);
        Assert.Equal(fornecedor.Identifier, item.Identifier);
        Assert.Equal(fornecedor.Id, item.Id);
    }

    [Fact]
    public async Task IdDeParceiroDeOutroTipoNaoTrazNadaAsync()
    {
        var cliente = await IncluirAsync(NovoParceiro(cliente: true, fornecedor: false));

        var pagina = await PesquisarAsync($"isSupplier=true&id={cliente.Id}");

        Assert.Empty(pagina.Items);
        Assert.Equal(0, pagina.TotalCount);
    }

    [Fact]
    public async Task SegundaPaginaAsync()
    {
        for (var indice = 1; indice <= 25; indice++)
        {
            await IncluirAsync(NovoParceiro(tradeName: $"Cliente {indice:00}"));
        }

        var pagina = await PesquisarAsync("isClient=true&page=2&pageSize=20");

        Assert.Equal(5, pagina.Items.Count);
        Assert.Equal(2, pagina.Page);
        Assert.Equal(20, pagina.PageSize);
        Assert.Equal(25, pagina.TotalCount);
        Assert.Equal(["Cliente 21", "Cliente 22", "Cliente 23", "Cliente 24", "Cliente 25"], pagina.Items.Select(item => item.TradeName));
    }

    [Fact]
    public async Task PaginacaoPadraoAsync()
    {
        await IncluirAsync(NovoParceiro());

        var pagina = await PesquisarAsync("isClient=true");

        Assert.Equal(1, pagina.Page);
        Assert.Equal(20, pagina.PageSize);
    }

    [Theory]
    [InlineData("pageSize=101", "PageSize")]
    [InlineData("pageSize=0", "PageSize")]
    [InlineData("page=0", "Page")]
    public async Task PaginacaoInvalidaRetorna400Async(string filtro, string chave)
    {
        using var resposta = await Cliente.GetAsync($"{Rota}?isClient=true&{filtro}", Cancelamento);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains(chave, await ChavesDeErroAsync(resposta));
    }

    [Fact]
    public async Task InativoForaDaPesquisaPadraoAsync()
    {
        var ativo = await IncluirAsync(NovoParceiro());
        var inativo = await IncluirAsync(NovoParceiro());
        await InativarAsync(inativo);

        var pagina = await PesquisarAsync("isClient=true");

        Assert.Equal([ativo.Identifier], pagina.Items.Select(item => item.Identifier));
    }

    [Fact]
    public async Task ListarInativosAsync()
    {
        await IncluirAsync(NovoParceiro());
        var inativo = await IncluirAsync(NovoParceiro());
        await InativarAsync(inativo);

        var pagina = await PesquisarAsync("isClient=true&isActive=false");

        var item = Assert.Single(pagina.Items);
        Assert.Equal(inativo.Identifier, item.Identifier);
        Assert.False(item.IsActive);
    }

    private async Task<PagedResult<PartnerSummaryResponse>> PesquisarAsync(string filtro)
    {
        using var resposta = await Cliente.GetAsync($"{Rota}?{filtro}", Cancelamento);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        return (await resposta.Content.ReadFromJsonAsync<PagedResult<PartnerSummaryResponse>>(Cancelamento))!;
    }

    private async Task InativarAsync(PartnerResponse parceiro)
    {
        using var resposta = await Cliente.DeleteAsync($"{Rota}/{parceiro.Identifier}", Cancelamento);
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
    }
}
