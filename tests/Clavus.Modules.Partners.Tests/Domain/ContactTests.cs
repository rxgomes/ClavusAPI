using Clavus.Modules.Partners.Domain;
using Clavus.Shared.Abstractions.Domain;

namespace Clavus.Modules.Partners.Tests.Domain;

/// <summary>
/// Cenários de validação de "Contatos do parceiro".
/// </summary>
public sealed class ContactTests
{
    [Fact]
    public void ContatoEGuardadoSemEspacosNasPontas()
    {
        var contato = new Contact(" Maria Souza ", " Compras ");

        Assert.Equal("Maria Souza", contato.Name);
        Assert.Equal("Compras", contato.Role);
    }

    [Fact]
    public void FuncaoEmBrancoFicaVazia()
    {
        var contato = new Contact("Maria Souza", "  ");

        Assert.Null(contato.Role);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ContatoSemNomeERejeitado(string? nome)
    {
        var erro = Assert.Throws<DomainException>(() => new Contact(nome));

        Assert.Equal("O campo nome do contato é obrigatório.", erro.Message);
    }

    [Fact]
    public void NomeLongoDemaisERejeitado()
    {
        var erro = Assert.Throws<DomainException>(() => new Contact(new string('a', 101)));

        Assert.Equal("O campo nome do contato deve ter no máximo 100 caracteres.", erro.Message);
    }

    [Fact]
    public void FuncaoLongaDemaisERejeitada()
    {
        var erro = Assert.Throws<DomainException>(() => new Contact("Maria Souza", new string('a', 31)));

        Assert.Equal("O campo função deve ter no máximo 30 caracteres.", erro.Message);
    }

    [Fact]
    public void AlteracaoInvalidaNaoMudaOContato()
    {
        var contato = new Contact("Maria Souza", "Compras");

        Assert.Throws<DomainException>(() => contato.Update("João", new string('a', 31)));

        Assert.Equal("Maria Souza", contato.Name);
        Assert.Equal("Compras", contato.Role);
    }

    [Fact]
    public void AdicionaERemoveEmailsETelefones()
    {
        var contato = new Contact("Maria Souza");
        var email = new Email("maria@ixact.com", "Comercial");
        var outroEmail = new Email("compras@ixact.com", "Compras");
        var telefone = new Phone("11", "987654321");

        contato.AddEmail(email);
        contato.AddEmail(outroEmail);
        contato.AddPhone(telefone);

        Assert.True(contato.RemoveEmail(email));
        Assert.True(contato.RemovePhone(telefone));
        Assert.Equal([outroEmail], contato.Emails);
        Assert.Empty(contato.Phones);
    }
}
