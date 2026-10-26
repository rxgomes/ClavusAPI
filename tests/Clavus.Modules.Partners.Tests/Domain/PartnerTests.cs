using Clavus.Modules.Partners.Domain;
using Clavus.Shared.Abstractions.Domain;

namespace Clavus.Modules.Partners.Tests.Domain;

/// <summary>
/// Cenários de "Dados obrigatórios do parceiro", "Inscrições municipal e estadual" e indicadores padrão.
/// </summary>
public sealed class PartnerTests
{
    private static readonly Document Cnpj = new("11222333000181");

    private static readonly Address Endereco = new("01310100");

    [Fact]
    public void ParceiroEGuardadoSemEspacosNasPontas()
    {
        var parceiro = new Partner(" Ixact Tecnologia Ltda ", " Ixact ", Cnpj, Endereco);

        Assert.Equal("Ixact Tecnologia Ltda", parceiro.Name);
        Assert.Equal("Ixact", parceiro.TradeName);
        Assert.Equal(Cnpj, parceiro.Document);
        Assert.Equal(Endereco, parceiro.Address);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RazaoSocialEmBrancoERejeitada(string? razaoSocial)
    {
        var erro = Assert.Throws<DomainException>(() => new Partner(razaoSocial, "Ixact", Cnpj, Endereco));

        Assert.Equal("O campo razão social é obrigatório.", erro.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NomeFantasiaEmBrancoERejeitado(string? nomeFantasia)
    {
        var erro = Assert.Throws<DomainException>(() => new Partner("Ixact Tecnologia Ltda", nomeFantasia, Cnpj, Endereco));

        Assert.Equal("O campo nome fantasia é obrigatório.", erro.Message);
    }

    [Fact]
    public void RazaoSocialLongaDemaisERejeitada()
    {
        var erro = Assert.Throws<DomainException>(() => new Partner(new string('a', 101), "Ixact", Cnpj, Endereco));

        Assert.Equal("O campo razão social deve ter no máximo 100 caracteres.", erro.Message);
    }

    [Fact]
    public void NomeFantasiaLongoDemaisERejeitado()
    {
        var erro = Assert.Throws<DomainException>(() => new Partner("Ixact Tecnologia Ltda", new string('a', 101), Cnpj, Endereco));

        Assert.Equal("O campo nome fantasia deve ter no máximo 100 caracteres.", erro.Message);
    }

    [Fact]
    public void ParceiroSemInscricoesFicaComInscricoesVazias()
    {
        var parceiro = new Partner("Ixact Tecnologia Ltda", "Ixact", Cnpj, Endereco, " ", "");

        Assert.Null(parceiro.MunicipalRegistration);
        Assert.Null(parceiro.StateRegistration);
    }

    [Fact]
    public void InscricoesSaoGuardadasSemEspacosNasPontas()
    {
        var parceiro = new Partner("Ixact Tecnologia Ltda", "Ixact", Cnpj, Endereco, " 123456 ", " 110.042.490.114 ");

        Assert.Equal("123456", parceiro.MunicipalRegistration);
        Assert.Equal("110.042.490.114", parceiro.StateRegistration);
    }

    [Fact]
    public void InscricaoEstadualLongaDemaisERejeitada()
    {
        var erro = Assert.Throws<DomainException>(
            () => new Partner("Ixact Tecnologia Ltda", "Ixact", Cnpj, Endereco, stateRegistration: new string('1', 16)));

        Assert.Equal("O campo inscrição estadual deve ter no máximo 15 caracteres.", erro.Message);
    }

    [Fact]
    public void InscricaoMunicipalLongaDemaisERejeitada()
    {
        var erro = Assert.Throws<DomainException>(
            () => new Partner("Ixact Tecnologia Ltda", "Ixact", Cnpj, Endereco, municipalRegistration: new string('1', 16)));

        Assert.Equal("O campo inscrição municipal deve ter no máximo 15 caracteres.", erro.Message);
    }

    [Fact]
    public void ParceiroNovoNaoEClienteNemFornecedorEEstaAtivo()
    {
        var parceiro = new Partner("Ixact Tecnologia Ltda", "Ixact", Cnpj, Endereco);

        Assert.False(parceiro.IsClient);
        Assert.False(parceiro.IsSupplier);
        Assert.True(parceiro.IsActive);
    }

    [Fact]
    public void RenomearInvalidoNaoMudaOParceiro()
    {
        var parceiro = new Partner("Ixact Tecnologia Ltda", "Ixact", Cnpj, Endereco);

        Assert.Throws<DomainException>(() => parceiro.Rename("Outra Razão", " "));

        Assert.Equal("Ixact Tecnologia Ltda", parceiro.Name);
        Assert.Equal("Ixact", parceiro.TradeName);
    }

    [Fact]
    public void AdicionaERemoveContatosEmailsETelefones()
    {
        var parceiro = new Partner("Ixact Tecnologia Ltda", "Ixact", Cnpj, Endereco);
        var contato = new Contact("Maria Souza");
        var email = new Email("contato@ixact.com", "Comercial");
        var telefone = new Phone("11", "33334444");

        parceiro.AddContact(contato);
        parceiro.AddEmail(email);
        parceiro.AddPhone(telefone);

        Assert.Equal([contato], parceiro.Contacts);
        Assert.Equal([email], parceiro.Emails);
        Assert.Equal([telefone], parceiro.Phones);

        Assert.True(parceiro.RemoveContact(contato));
        Assert.True(parceiro.RemoveEmail(email));
        Assert.True(parceiro.RemovePhone(telefone));
        Assert.False(parceiro.RemoveEmail(email));
        Assert.Empty(parceiro.Contacts);
        Assert.Empty(parceiro.Emails);
        Assert.Empty(parceiro.Phones);
    }
}
