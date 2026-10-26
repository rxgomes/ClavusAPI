using Clavus.Modules.Partners.Domain;
using Clavus.Shared.Abstractions.Domain;

namespace Clavus.Modules.Partners.Tests.Domain;

/// <summary>
/// Cenários de "Documento CPF ou CNPJ", "Validação do CPF" e "Validação do CNPJ alfanumérico".
/// </summary>
public sealed class DocumentTests
{
    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("52998224725", "52998224725")]
    public void CpfEGuardadoSemFormatacaoComoPessoaFisica(string informado, string esperado)
    {
        var documento = new Document(informado);

        Assert.Equal(esperado, documento.Value);
        Assert.Equal(DocumentType.Individual, documento.Type);
    }

    [Theory]
    [InlineData("11.222.333/0001-81", "11222333000181")]
    [InlineData("12.ABC.345/01DE-35", "12ABC34501DE35")]
    [InlineData("12abc34501de35", "12ABC34501DE35")]
    [InlineData(" 12 ABC 345 01DE 35 ", "12ABC34501DE35")]
    public void CnpjEGuardadoSemFormatacaoEmMaiusculasComoPessoaJuridica(string informado, string esperado)
    {
        var documento = new Document(informado);

        Assert.Equal(esperado, documento.Value);
        Assert.Equal(DocumentType.Company, documento.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" . - / ")]
    public void DocumentoVazioERejeitado(string? informado)
    {
        var erro = Assert.Throws<DomainException>(() => new Document(informado));

        Assert.Equal("O campo documento é obrigatório.", erro.Message);
    }

    [Theory]
    [InlineData("5299822472")]
    [InlineData("529982247250")]
    [InlineData("1122233300018")]
    [InlineData("112223330001810")]
    public void DocumentoComTamanhoInvalidoERejeitado(string informado)
    {
        var erro = Assert.Throws<DomainException>(() => new Document(informado));

        Assert.StartsWith("Documento inválido", erro.Message);
    }

    [Theory]
    [InlineData("52998224726")]
    [InlineData("52998224715")]
    [InlineData("111.111.111-11")]
    [InlineData("00000000000")]
    [InlineData("5299822472A")]
    [InlineData("A2998224725")]
    public void CpfInvalidoERejeitado(string informado)
    {
        var erro = Assert.Throws<DomainException>(() => new Document(informado));

        Assert.Equal("CPF inválido.", erro.Message);
    }

    [Theory]
    [InlineData("12ABC34501DE36")]
    [InlineData("11222333000182")]
    [InlineData("12ABC34501DEA5")]
    [InlineData("12ABC34501DE3A")]
    [InlineData("00000000000000")]
    [InlineData("AAAAAAAAAAAAAA")]
    [InlineData("12ABC34501D#35")]
    public void CnpjInvalidoERejeitado(string informado)
    {
        var erro = Assert.Throws<DomainException>(() => new Document(informado));

        Assert.Equal("CNPJ inválido.", erro.Message);
    }

    [Fact]
    public void DocumentosComMesmoValorSaoIguais()
    {
        Assert.Equal(new Document("529.982.247-25"), new Document("52998224725"));
    }
}
