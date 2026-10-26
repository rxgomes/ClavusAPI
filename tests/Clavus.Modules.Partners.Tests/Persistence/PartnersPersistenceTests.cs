using Clavus.Modules.Partners.Domain;
using Clavus.Modules.Partners.Persistence;
using Clavus.Shared.Infrastructure.Persistence;
using Clavus.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Npgsql;

[assembly: AssemblyFixture(typeof(PostgresFixture))]

namespace Clavus.Modules.Partners.Tests.Persistence;

/// <summary>
/// Cenários de persistência da capability <c>partners</c>, no banco temporário com a migration do módulo.
/// </summary>
public sealed class PartnersPersistenceTests(PostgresFixture postgres) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task ParceiroCompletoEGravadoELidoAsync()
    {
        var parceiro = NovoParceiro(new Address("01310-100", "Av.", "Paulista", "1000", "Bela Vista", "São Paulo", "São Paulo", "sp"));
        parceiro.ChangeRegistrations("123456", "110042490114");
        parceiro.IsClient = true;
        parceiro.AddEmail(new Email("contato@ixact.com", "Comercial"));
        parceiro.AddPhone(new Phone("11", "33334444"));
        var contato = new Contact("Maria Souza", "Compras");
        contato.AddEmail(new Email("maria@ixact.com", "Pessoal"));
        contato.AddPhone(new Phone("11", "987654321"));
        parceiro.AddContact(contato);

        await IncluirAsync(parceiro);
        var lido = await LerAsync(parceiro.Id);

        Assert.Equal(parceiro.Name, lido.Name);
        Assert.Equal(parceiro.TradeName, lido.TradeName);
        Assert.Equal(parceiro.Document, lido.Document);
        Assert.Equal(parceiro.Address, lido.Address);
        Assert.Equal("SP", lido.Address.StateCode);
        Assert.Equal("123456", lido.MunicipalRegistration);
        Assert.Equal("110042490114", lido.StateRegistration);
        Assert.True(lido.IsClient);
        Assert.Equal(["contato@ixact.com"], lido.Emails.Select(email => email.Address));
        Assert.Equal(["33334444"], lido.Phones.Select(phone => phone.Number));

        var contatoLido = Assert.Single(lido.Contacts);
        Assert.Equal("Maria Souza", contatoLido.Name);
        Assert.Equal("Compras", contatoLido.Role);
        Assert.Equal(["maria@ixact.com"], contatoLido.Emails.Select(email => email.Address));
        Assert.Equal(["987654321"], contatoLido.Phones.Select(phone => phone.Number));
    }

    [Fact]
    public async Task DocumentoEGravadoSemFormatacaoAsync()
    {
        var cpf = Documentos.NovoCpf();
        var formatado = $"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}";
        var parceiro = new Partner("Fulano de Tal", "Fulano", new Document(formatado), new Address("01310100"));

        await IncluirAsync(parceiro);

        Assert.Equal(cpf, await LerColunaAsync<string>("select document from partners.partner where id = $1", parceiro.Id));
        Assert.Equal(DocumentType.Individual, (await LerAsync(parceiro.Id)).Document.Type);
    }

    [Fact]
    public async Task DocumentoDuplicadoERejeitadoAsync()
    {
        var cpf = Documentos.NovoCpf();
        await IncluirAsync(new Partner("Primeiro", "Primeiro", new Document(cpf), new Address("01310100")));
        var duplicado = new Partner("Segundo", "Segundo", new Document($"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}"), new Address("01310100"));

        var erro = await Assert.ThrowsAsync<DbUpdateException>(() => IncluirAsync(duplicado));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(erro.InnerException).SqlState);
    }

    [Fact]
    public async Task IndicadoresSaoFalsosPorPadraoAsync()
    {
        var parceiro = NovoParceiro();

        await IncluirAsync(parceiro);
        var lido = await LerAsync(parceiro.Id);

        Assert.False(lido.IsClient);
        Assert.False(lido.IsSupplier);
    }

    [Fact]
    public async Task ParceiroClienteEFornecedorEGravadoAsync()
    {
        var parceiro = NovoParceiro();
        parceiro.IsClient = true;
        parceiro.IsSupplier = true;

        await IncluirAsync(parceiro);
        var lido = await LerAsync(parceiro.Id);

        Assert.True(lido.IsClient);
        Assert.True(lido.IsSupplier);
    }

    [Theory]
    [InlineData("email", "address, type", "'sem.dono@ixact.com', 'Comercial'")]
    [InlineData("phone", "area_code, number", "'11', '33334444'")]
    public async Task ItemSemDonoOuComDoisDonosERejeitadoPeloBancoAsync(string tabela, string colunas, string valores)
    {
        var parceiro = NovoParceiro();
        parceiro.AddContact(new Contact("Maria Souza"));
        await IncluirAsync(parceiro);
        var contatoId = parceiro.Contacts.Single().Id;

        var semDono = await Assert.ThrowsAsync<PostgresException>(
            () => ExecutarAsync($"insert into partners.{tabela} ({colunas}) values ({valores})"));
        var doisDonos = await Assert.ThrowsAsync<PostgresException>(
            () => ExecutarAsync($"insert into partners.{tabela} ({colunas}, partner_id, contact_id) values ({valores}, {parceiro.Id}, {contatoId})"));

        Assert.Equal(PostgresErrorCodes.CheckViolation, semDono.SqlState);
        Assert.Equal(PostgresErrorCodes.CheckViolation, doisDonos.SqlState);
    }

    [Fact]
    public async Task RemoverEmailETelefoneExcluiApenasOsItensRemovidosAsync()
    {
        var parceiro = NovoParceiro();
        parceiro.AddEmail(new Email("removido@ixact.com", "Comercial"));
        parceiro.AddEmail(new Email("mantido@ixact.com", "Comercial"));
        parceiro.AddPhone(new Phone("11", "33330001"));
        parceiro.AddPhone(new Phone("11", "33330002"));
        var contato = new Contact("Maria Souza");
        contato.AddEmail(new Email("maria.removido@ixact.com", "Pessoal"));
        contato.AddEmail(new Email("maria.mantido@ixact.com", "Pessoal"));
        contato.AddPhone(new Phone("11", "987650001"));
        contato.AddPhone(new Phone("11", "987650002"));
        parceiro.AddContact(contato);
        await IncluirAsync(parceiro);

        await using (var contexto = CriarContexto())
        {
            var rastreado = await CarregarAsync(contexto, parceiro.Id);
            var contatoRastreado = rastreado.Contacts.Single();
            rastreado.RemoveEmail(rastreado.Emails.Single(email => email.Address == "removido@ixact.com"));
            rastreado.RemovePhone(rastreado.Phones.Single(phone => phone.Number == "33330001"));
            contatoRastreado.RemoveEmail(contatoRastreado.Emails.Single(email => email.Address == "maria.removido@ixact.com"));
            contatoRastreado.RemovePhone(contatoRastreado.Phones.Single(phone => phone.Number == "987650001"));
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var lido = await LerAsync(parceiro.Id);
        var contatoLido = lido.Contacts.Single();

        Assert.Equal(["mantido@ixact.com"], lido.Emails.Select(email => email.Address));
        Assert.Equal(["33330002"], lido.Phones.Select(phone => phone.Number));
        Assert.Equal(["maria.mantido@ixact.com"], contatoLido.Emails.Select(email => email.Address));
        Assert.Equal(["987650002"], contatoLido.Phones.Select(phone => phone.Number));
        Assert.Equal(0, await ContarAsync("email", "address like '%removido@ixact.com'"));
        Assert.Equal(0, await ContarAsync("phone", "number in ('33330001', '987650001')"));
    }

    [Fact]
    public async Task RemoverContatoExcluiOContatoESeusItensAsync()
    {
        var parceiro = NovoParceiro();
        parceiro.AddEmail(new Email("contato@ixact.com", "Comercial"));
        parceiro.AddPhone(new Phone("11", "33334444"));
        var contato = new Contact("Maria Souza");
        contato.AddEmail(new Email("maria@ixact.com", "Pessoal"));
        contato.AddPhone(new Phone("11", "987654321"));
        parceiro.AddContact(contato);
        await IncluirAsync(parceiro);
        var contatoId = contato.Id;

        await using (var contexto = CriarContexto())
        {
            var rastreado = await CarregarAsync(contexto, parceiro.Id);
            rastreado.RemoveContact(rastreado.Contacts.Single());
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var lido = await LerAsync(parceiro.Id);

        Assert.Empty(lido.Contacts);
        Assert.Single(lido.Emails);
        Assert.Single(lido.Phones);
        Assert.Equal(0, await ContarAsync("contact", $"id = {contatoId}"));
        Assert.Equal(0, await ContarAsync("email", $"contact_id = {contatoId}"));
        Assert.Equal(0, await ContarAsync("phone", $"contact_id = {contatoId}"));
    }

    [Fact]
    public async Task ExcluirParceiroExcluiContatosEmailsETelefonesAsync()
    {
        var parceiro = NovoParceiro();
        parceiro.AddEmail(new Email("contato@ixact.com", "Comercial"));
        parceiro.AddPhone(new Phone("11", "33334444"));
        var contato = new Contact("Maria Souza");
        contato.AddEmail(new Email("maria@ixact.com", "Pessoal"));
        contato.AddPhone(new Phone("11", "987654321"));
        parceiro.AddContact(contato);
        await IncluirAsync(parceiro);
        var contatoId = contato.Id;

        await using (var contexto = CriarContexto())
        {
            var rastreado = await contexto.Set<Partner>().SingleAsync(p => p.Id == parceiro.Id, TestContext.Current.CancellationToken);
            contexto.Remove(rastreado);
            await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(0, await ContarAsync("partner", $"id = {parceiro.Id}"));
        Assert.Equal(0, await ContarAsync("contact", $"partner_id = {parceiro.Id}"));
        Assert.Equal(0, await ContarAsync("email", $"partner_id = {parceiro.Id} or contact_id = {contatoId}"));
        Assert.Equal(0, await ContarAsync("phone", $"partner_id = {parceiro.Id} or contact_id = {contatoId}"));
    }

    private static Partner NovoParceiro(Address? endereco = null)
    {
        return new Partner("Ixact Tecnologia Ltda", "Ixact", new Document(Documentos.NovoCpf()), endereco ?? new Address("01310100"));
    }

    private PartnersDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<PartnersDbContext>();
        DatabaseServiceCollectionExtensions.ConfigureModuleDbContext(options, postgres.DataSource, PartnersDbContext.SchemaName);
        return new PartnersDbContext(options.Options);
    }

    private async Task IncluirAsync(Partner parceiro)
    {
        await using var contexto = CriarContexto();
        contexto.Add(parceiro);
        await contexto.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<Partner> LerAsync(long id)
    {
        await using var contexto = CriarContexto();
        return await CarregarAsync(contexto, id);
    }

    private static Task<Partner> CarregarAsync(PartnersDbContext contexto, long id)
    {
        return contexto.Set<Partner>()
            .Include(partner => partner.Emails)
            .Include(partner => partner.Phones)
            .Include(partner => partner.Contacts).ThenInclude(contact => contact.Emails)
            .Include(partner => partner.Contacts).ThenInclude(contact => contact.Phones)
            .AsSplitQuery()
            .SingleAsync(partner => partner.Id == id, TestContext.Current.CancellationToken);
    }

    private async Task ExecutarAsync(string sql)
    {
        await using var comando = postgres.DataSource.CreateCommand(sql);
        await comando.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private async Task<long> ContarAsync(string tabela, string condicao)
    {
        return await LerColunaAsync<long>($"select count(*) from partners.{tabela} where {condicao}");
    }

    private async Task<T> LerColunaAsync<T>(string sql, params object[] parametros)
    {
        await using var comando = postgres.DataSource.CreateCommand(sql);
        foreach (var parametro in parametros)
        {
            comando.Parameters.Add(new NpgsqlParameter { Value = parametro });
        }

        return (T)(await comando.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }
}
