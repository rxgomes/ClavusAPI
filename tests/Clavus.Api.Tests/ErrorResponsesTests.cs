using System.Net;
using System.Text;
using System.Text.Json;
using Clavus.Tests.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Clavus.Api.Tests;

/// <summary>
/// Cenários da capability <c>error-responses</c>.
/// </summary>
public sealed class ErrorResponsesTests(PostgresFixture postgres)
{
    private const string ProblemJson = "application/problem+json";

    [Theory]
    [InlineData("GET", "/rota-que-nao-existe", HttpStatusCode.NotFound)]
    [InlineData("DELETE", "/__testes/somente-get", HttpStatusCode.MethodNotAllowed)]
    [InlineData("GET", "/__testes/excecao", HttpStatusCode.InternalServerError)]
    public async Task ErroTemCamposObrigatoriosEStatusIgualAoHttpAsync(string metodo, string caminho, HttpStatusCode esperado)
    {
        var (resposta, corpo) = await EnviarAsync(Environments.Production, new HttpRequestMessage(new HttpMethod(metodo), caminho));

        Assert.Equal(esperado, resposta.StatusCode);
        Assert.Equal(ProblemJson, resposta.Content.Headers.ContentType?.MediaType);
        Assert.False(string.IsNullOrWhiteSpace(corpo.RootElement.GetProperty("type").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(corpo.RootElement.GetProperty("title").GetString()));
        Assert.Equal((int)esperado, corpo.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(corpo.RootElement.GetProperty("traceId").GetString()));
    }

    [Theory]
    [InlineData("GET", "/rota-que-nao-existe", "Recurso não encontrado.")]
    [InlineData("DELETE", "/__testes/somente-get", "Método não permitido.")]
    [InlineData("GET", "/__testes/excecao", "Ocorreu um erro inesperado.")]
    public async Task TituloEmPortuguesAsync(string metodo, string caminho, string titulo)
    {
        var (_, corpo) = await EnviarAsync(Environments.Production, new HttpRequestMessage(new HttpMethod(metodo), caminho));

        Assert.Equal(titulo, corpo.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ExcecaoEmProductionNaoExpoeDetalhesAsync()
    {
        var (resposta, corpo) = await EnviarAsync(Environments.Production, new HttpRequestMessage(HttpMethod.Get, "/__testes/excecao"));
        var texto = corpo.RootElement.GetRawText();

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.DoesNotContain(EndpointsDeTeste.MensagemDaExcecao, texto);
        Assert.DoesNotContain(nameof(InvalidOperationException), texto);
        Assert.DoesNotContain("   at ", texto);
        Assert.DoesNotContain("   em ", texto);
    }

    [Fact]
    public async Task ExcecaoEmDevelopmentTrazMensagemNoDetailAsync()
    {
        var (resposta, corpo) = await EnviarAsync(Environments.Development, new HttpRequestMessage(HttpMethod.Get, "/__testes/excecao"));

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.Equal(ProblemJson, resposta.Content.Headers.ContentType?.MediaType);
        Assert.Contains(EndpointsDeTeste.MensagemDaExcecao, corpo.RootElement.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task JsonMalformadoRetorna400Async(string ambiente)
    {
        var requisicao = new HttpRequestMessage(HttpMethod.Post, "/__testes/validacao")
        {
            Content = new StringContent("{\"nome\": ", Encoding.UTF8, "application/json"),
        };

        var (resposta, corpo) = await EnviarAsync(ambiente, requisicao);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(ProblemJson, resposta.Content.Headers.ContentType?.MediaType);
        Assert.Equal(400, corpo.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task ValidacaoRetornaErrosPorCampoAsync()
    {
        var requisicao = new HttpRequestMessage(HttpMethod.Post, "/__testes/validacao")
        {
            Content = new StringContent("{\"quantidade\": 50}", Encoding.UTF8, "application/json"),
        };

        var (resposta, corpo) = await EnviarAsync(Environments.Production, requisicao);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(ProblemJson, resposta.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Um ou mais campos são inválidos.", corpo.RootElement.GetProperty("title").GetString());

        var erros = corpo.RootElement.GetProperty("errors");
        Assert.Equal(["O nome é obrigatório."], erros.GetProperty("Nome").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(["A quantidade deve estar entre 1 e 10."], erros.GetProperty("Quantidade").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task HealthCheckIndisponivelMantemFormatoTextoAsync()
    {
        await using var factory = new ClavusApiFactory(Environments.Production, ClavusApiFactory.BancoInacessivel);
        using var cliente = factory.CreateClient();

        using var resposta = await cliente.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        var corpo = await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        Assert.Equal("Unhealthy", corpo);
        Assert.NotEqual(ProblemJson, resposta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task TraceIdDaRespostaApareceNoLogDoErroAsync()
    {
        var logs = new ColetorDeLogs();

        var (_, corpo) = await EnviarAsync(Environments.Production, new HttpRequestMessage(HttpMethod.Get, "/__testes/excecao"), logs);
        var traceIdDaResposta = corpo.RootElement.GetProperty("traceId").GetString()!;

        var erro = Assert.Single(logs.Registros, r => r.Nivel == LogLevel.Error && r.Excecao is InvalidOperationException);
        var traceIdDoLog = Assert.IsType<string>(erro.Escopos["TraceId"]);
        Assert.Contains(traceIdDoLog, traceIdDaResposta);
    }

    private async Task<(HttpResponseMessage Resposta, JsonDocument Corpo)> EnviarAsync(
        string ambiente,
        HttpRequestMessage requisicao,
        ColetorDeLogs? logs = null)
    {
        await using var factory = new ClavusApiFactory(ambiente, postgres.ConnectionString, logs: logs);
        using var cliente = factory.CreateClient();

        using (requisicao)
        {
            var resposta = await cliente.SendAsync(requisicao, TestContext.Current.CancellationToken);
            var texto = await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            return (resposta, JsonDocument.Parse(texto));
        }
    }
}
