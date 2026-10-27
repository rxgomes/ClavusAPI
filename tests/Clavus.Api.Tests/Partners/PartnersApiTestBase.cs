using System.Net.Http.Json;
using System.Text.Json;
using Clavus.Modules.Partners.Contracts;
using Clavus.Modules.Partners.Persistence;
using Clavus.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Clavus.Api.Tests.Partners;

/// <summary>
/// Base dos testes dos endpoints <c>/partners</c>: sobe a API em <c>Production</c> sobre o banco temporário,
/// aplica a migration do módulo e esvazia as tabelas antes de cada teste.
/// </summary>
/// <remarks>
/// As classes derivadas ficam na mesma coleção para não rodarem em paralelo, pois todas usam as mesmas tabelas.
/// </remarks>
public abstract class PartnersApiTestBase(PostgresFixture postgres) : IAsyncLifetime
{
    public const string Colecao = "PartnersApi";

    protected const string Rota = "/partners";

    private ClavusApiFactory? _factory;

    protected PostgresFixture Postgres { get; } = postgres;

    protected HttpClient Cliente { get; private set; } = null!;

    protected static CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _factory = new ClavusApiFactory(Environments.Production, Postgres.ConnectionString);
        Cliente = _factory.CreateClient();

        await using (var escopo = _factory.Services.CreateAsyncScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<PartnersDbContext>();
            await contexto.Database.MigrateAsync(Cancelamento);
        }

        await using var limpar = Postgres.DataSource.CreateCommand(
            "truncate partners.email, partners.phone, partners.contact, partners.partner restart identity cascade");
        await limpar.ExecuteNonQueryAsync(Cancelamento);
    }

    public async ValueTask DisposeAsync()
    {
        Cliente.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Requisição de inclusão válida, com o mínimo obrigatório
    /// </summary>
    protected static CreatePartnerRequest NovoParceiro(bool cliente = true, bool fornecedor = false, string? documento = null, string tradeName = "Ixact")
    {
        return new CreatePartnerRequest
        {
            Name = "Ixact Tecnologia Ltda",
            TradeName = tradeName,
            Document = documento ?? Documentos.NovoCpf(),
            Address = new AddressRequest { ZipCode = "01310-100", City = "São Paulo", StateCode = "SP" },
            IsClient = cliente,
            IsSupplier = fornecedor,
        };
    }

    /// <summary>
    /// Requisição de alteração com todos os dados atuais do parceiro, incluindo o identifier dos contatos
    /// </summary>
    protected static UpdatePartnerRequest ParaAlteracao(PartnerResponse parceiro)
    {
        return new UpdatePartnerRequest
        {
            Name = parceiro.Name,
            TradeName = parceiro.TradeName,
            Document = parceiro.Document,
            MunicipalRegistration = parceiro.MunicipalRegistration,
            StateRegistration = parceiro.StateRegistration,
            Address = new AddressRequest
            {
                ZipCode = parceiro.Address.ZipCode,
                StreetType = parceiro.Address.StreetType,
                Street = parceiro.Address.Street,
                Number = parceiro.Address.Number,
                Neighborhood = parceiro.Address.Neighborhood,
                City = parceiro.Address.City,
                State = parceiro.Address.State,
                StateCode = parceiro.Address.StateCode,
            },
            IsClient = parceiro.IsClient,
            IsSupplier = parceiro.IsSupplier,
            IsActive = parceiro.IsActive,
            Emails = parceiro.Emails.Select(ParaRequisicao).ToList(),
            Phones = parceiro.Phones.Select(ParaRequisicao).ToList(),
            Contacts = parceiro.Contacts
                .Select(contato => new ContactRequest
                {
                    Identifier = contato.Identifier,
                    Name = contato.Name,
                    Role = contato.Role,
                    Emails = contato.Emails.Select(ParaRequisicao).ToList(),
                    Phones = contato.Phones.Select(ParaRequisicao).ToList(),
                })
                .ToList(),
        };
    }

    protected static EmailRequest ParaRequisicao(EmailResponse email)
    {
        return new EmailRequest { Address = email.Address, Type = email.Type };
    }

    protected static PhoneRequest ParaRequisicao(PhoneResponse telefone)
    {
        return new PhoneRequest { AreaCode = telefone.AreaCode, Number = telefone.Number };
    }

    /// <summary>
    /// Inclui o parceiro e devolve a resposta, exigindo <c>201</c>
    /// </summary>
    protected async Task<PartnerResponse> IncluirAsync(CreatePartnerRequest requisicao)
    {
        using var resposta = await Cliente.PostAsJsonAsync(Rota, requisicao, Cancelamento);
        Assert.Equal(System.Net.HttpStatusCode.Created, resposta.StatusCode);

        return (await resposta.Content.ReadFromJsonAsync<PartnerResponse>(Cancelamento))!;
    }

    /// <summary>
    /// Consulta o parceiro completo pelo identifier, exigindo <c>200</c>
    /// </summary>
    protected async Task<PartnerResponse> DetalharAsync(PartnerResponse parceiro)
    {
        var tipo = parceiro.IsClient ? "isClient=true" : "isSupplier=true";
        using var resposta = await Cliente.GetAsync($"{Rota}/details?{tipo}&identifier={parceiro.Identifier}", Cancelamento);
        Assert.Equal(System.Net.HttpStatusCode.OK, resposta.StatusCode);

        return (await resposta.Content.ReadFromJsonAsync<PartnerResponse>(Cancelamento))!;
    }

    protected static async Task<JsonDocument> LerJsonAsync(HttpResponseMessage resposta)
    {
        await using var conteudo = await resposta.Content.ReadAsStreamAsync(Cancelamento);
        return await JsonDocument.ParseAsync(conteudo, cancellationToken: Cancelamento);
    }

    /// <summary>
    /// Chaves do campo <c>errors</c> de uma resposta de validação
    /// </summary>
    protected static async Task<IReadOnlyList<string>> ChavesDeErroAsync(HttpResponseMessage resposta)
    {
        using var corpo = await LerJsonAsync(resposta);
        return corpo.RootElement.GetProperty("errors").EnumerateObject().Select(erro => erro.Name).ToList();
    }

    protected async Task<long> ContarAsync(string tabela)
    {
        await using var comando = Postgres.DataSource.CreateCommand($"select count(*) from partners.{tabela}");
        return (long)(await comando.ExecuteScalarAsync(Cancelamento))!;
    }
}
