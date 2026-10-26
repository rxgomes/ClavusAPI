using Clavus.Modules.Partners.Domain;
using Clavus.Shared.Abstractions.Domain;

namespace Clavus.Modules.Partners.Tests.Domain;

/// <summary>
/// Cenários de validação de "E-mails do parceiro e do contato".
/// </summary>
public sealed class EmailTests
{
    [Fact]
    public void EmailValidoEGuardadoSemEspacosNasPontas()
    {
        var email = new Email(" financeiro@ixact.com.br ", " Financeiro ");

        Assert.Equal("financeiro@ixact.com.br", email.Address);
        Assert.Equal("Financeiro", email.Type);
    }

    [Theory]
    [InlineData("financeiro.ixact.com")]
    [InlineData("financeiro@")]
    [InlineData("@ixact.com")]
    [InlineData("Financeiro <financeiro@ixact.com>")]
    public void EmailEmFormatoInvalidoERejeitado(string endereco)
    {
        var erro = Assert.Throws<DomainException>(() => new Email(endereco, "Comercial"));

        Assert.Equal("E-mail inválido.", erro.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void EmailVazioERejeitado(string? endereco)
    {
        var erro = Assert.Throws<DomainException>(() => new Email(endereco, "Comercial"));

        Assert.Equal("O campo e-mail é obrigatório.", erro.Message);
    }

    [Fact]
    public void EmailLongoDemaisERejeitado()
    {
        var endereco = new string('a', 66) + "@ixact.com";

        var erro = Assert.Throws<DomainException>(() => new Email(endereco, "Comercial"));

        Assert.Equal("O campo e-mail deve ter no máximo 75 caracteres.", erro.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void EmailSemTipoERejeitado(string? tipo)
    {
        var erro = Assert.Throws<DomainException>(() => new Email("financeiro@ixact.com", tipo));

        Assert.Equal("O campo tipo do e-mail é obrigatório.", erro.Message);
    }

    [Fact]
    public void TipoLongoDemaisERejeitado()
    {
        var erro = Assert.Throws<DomainException>(() => new Email("financeiro@ixact.com", new string('a', 16)));

        Assert.Equal("O campo tipo do e-mail deve ter no máximo 15 caracteres.", erro.Message);
    }
}
