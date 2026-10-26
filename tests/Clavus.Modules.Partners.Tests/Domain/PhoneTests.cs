using Clavus.Modules.Partners.Domain;
using Clavus.Shared.Abstractions.Domain;

namespace Clavus.Modules.Partners.Tests.Domain;

/// <summary>
/// Cenários de validação de "Telefones do parceiro e do contato".
/// </summary>
public sealed class PhoneTests
{
    [Fact]
    public void CelularFormatadoEGuardadoSemFormatacao()
    {
        var telefone = new Phone("(11)", "98765-4321");

        Assert.Equal("11", telefone.AreaCode);
        Assert.Equal("987654321", telefone.Number);
    }

    [Fact]
    public void TelefoneFixoEGuardadoSemFormatacao()
    {
        var telefone = new Phone("47", "3333-4444");

        Assert.Equal("33334444", telefone.Number);
    }

    [Theory]
    [InlineData("01")]
    [InlineData("10")]
    [InlineData("1")]
    [InlineData("111")]
    [InlineData("1A")]
    public void DddInvalidoERejeitado(string ddd)
    {
        var erro = Assert.Throws<DomainException>(() => new Phone(ddd, "987654321"));

        Assert.Equal("DDD inválido: informe 2 dígitos entre 11 e 99.", erro.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("( )")]
    public void DddVazioERejeitado(string? ddd)
    {
        var erro = Assert.Throws<DomainException>(() => new Phone(ddd, "987654321"));

        Assert.Equal("O campo DDD é obrigatório.", erro.Message);
    }

    [Theory]
    [InlineData("333-4444")]
    [InlineData("9876-543210")]
    [InlineData("9876-5432A")]
    public void NumeroComTamanhoInvalidoERejeitado(string numero)
    {
        var erro = Assert.Throws<DomainException>(() => new Phone("11", numero));

        Assert.Equal("Número de telefone inválido: informe 8 ou 9 dígitos.", erro.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" - ")]
    public void NumeroVazioERejeitado(string? numero)
    {
        var erro = Assert.Throws<DomainException>(() => new Phone("11", numero));

        Assert.Equal("O campo número do telefone é obrigatório.", erro.Message);
    }
}
