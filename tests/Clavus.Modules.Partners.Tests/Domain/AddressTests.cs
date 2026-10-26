using Clavus.Modules.Partners.Domain;
using Clavus.Shared.Abstractions.Domain;

namespace Clavus.Modules.Partners.Tests.Domain;

/// <summary>
/// Cenários de "Endereço do parceiro" e "UF do endereço".
/// </summary>
public sealed class AddressTests
{
    [Theory]
    [InlineData("01310-100")]
    [InlineData("01.310-100")]
    [InlineData("01310100")]
    public void CepEGuardadoSemFormatacao(string cep)
    {
        var endereco = new Address(cep);

        Assert.Equal("01310100", endereco.ZipCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void CepVazioERejeitado(string? cep)
    {
        var erro = Assert.Throws<DomainException>(() => new Address(cep));

        Assert.Equal("O campo CEP é obrigatório.", erro.Message);
    }

    [Theory]
    [InlineData("0131010")]
    [InlineData("013101000")]
    [InlineData("01310-10A")]
    public void CepInvalidoERejeitado(string cep)
    {
        var erro = Assert.Throws<DomainException>(() => new Address(cep));

        Assert.Equal("CEP inválido: informe 8 dígitos.", erro.Message);
    }

    [Fact]
    public void EnderecoSoComCepDeixaOsDemaisCamposVazios()
    {
        var endereco = new Address("01310100", " ", "", null, " ", null, "", " ");

        Assert.Null(endereco.StreetType);
        Assert.Null(endereco.Street);
        Assert.Null(endereco.Number);
        Assert.Null(endereco.Neighborhood);
        Assert.Null(endereco.City);
        Assert.Null(endereco.State);
        Assert.Null(endereco.StateCode);
    }

    [Fact]
    public void CamposSaoGuardadosSemEspacosNasPontas()
    {
        var endereco = new Address("01310100", " Av. ", " Paulista ", " 1000 ", " Bela Vista ", " São Paulo ", " São Paulo ", " sp ");

        Assert.Equal("Av.", endereco.StreetType);
        Assert.Equal("Paulista", endereco.Street);
        Assert.Equal("1000", endereco.Number);
        Assert.Equal("Bela Vista", endereco.Neighborhood);
        Assert.Equal("São Paulo", endereco.City);
        Assert.Equal("São Paulo", endereco.State);
        Assert.Equal("SP", endereco.StateCode);
    }

    [Fact]
    public void LogradouroLongoDemaisERejeitado()
    {
        var erro = Assert.Throws<DomainException>(() => new Address("01310100", street: new string('a', 101)));

        Assert.Equal("O campo logradouro deve ter no máximo 100 caracteres.", erro.Message);
    }

    [Theory]
    [InlineData("tipo de logradouro", 6)]
    [InlineData("número", 11)]
    [InlineData("bairro", 101)]
    [InlineData("cidade", 101)]
    [InlineData("estado", 36)]
    public void CampoLongoDemaisERejeitado(string campo, int tamanho)
    {
        var valor = new string('a', tamanho);

        var erro = Assert.Throws<DomainException>(() => campo switch
        {
            "tipo de logradouro" => new Address("01310100", streetType: valor),
            "número" => new Address("01310100", number: valor),
            "bairro" => new Address("01310100", neighborhood: valor),
            "cidade" => new Address("01310100", city: valor),
            _ => new Address("01310100", state: valor),
        });

        Assert.Equal($"O campo {campo} deve ter no máximo {tamanho - 1} caracteres.", erro.Message);
    }

    [Theory]
    [InlineData("XX")]
    [InlineData("S1")]
    public void UfInexistenteERejeitada(string uf)
    {
        var erro = Assert.Throws<DomainException>(() => new Address("01310100", stateCode: uf));

        Assert.Equal("UF inválida.", erro.Message);
    }

    [Fact]
    public void UfComMaisDeDoisCaracteresERejeitada()
    {
        Assert.Throws<DomainException>(() => new Address("01310100", stateCode: "SPX"));
    }

    [Fact]
    public void EnderecosComMesmosDadosSaoIguais()
    {
        Assert.Equal(new Address("01310-100", stateCode: "sp"), new Address("01310100", stateCode: "SP"));
    }
}
