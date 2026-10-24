# Clavus API

Backend do sistema Clavus: monolito modular em .NET 10 com PostgreSQL e EF Core. O front-end fica em um repositório separado (`..\UI`).

Os padrões de código estão em [`CLAUDE.md`](CLAUDE.md) e as regras de arquitetura e especificação em [`openspec/config.yaml`](openspec/config.yaml).

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (versão fixada em `global.json`)
- PostgreSQL 16 ou superior, instalado na máquina **ou** via Docker
- Ferramenta do EF Core: `dotnet tool install --global dotnet-ef`

## Banco de dados

A API e os testes usam o usuário `clavus` / senha `clavus` em `localhost:5432`. Essas credenciais valem apenas para desenvolvimento.

**PostgreSQL instalado na máquina.** Execute uma vez, como superusuário:

```bash
psql -h localhost -U postgres -f scripts/criar-usuario-dev.sql
```

O script cria o usuário `clavus` (com permissão `CREATEDB`, usada pelos testes) e o banco `clavus`.

**Docker.** Alternativa equivalente:

```bash
docker compose up -d
```

## Executar a API

```bash
dotnet run --project src/Clavus.Api
```

No Rider, use a run configuration **`Clavus.Api: http`**, que vem do `launchSettings.json`. Uma configuração sem esse perfil sobe a API como Production: a documentação não é exposta e a connection string de desenvolvimento não é carregada. No console deve aparecer `Hosting environment: Development`.

| Endpoint | Descrição |
|---|---|
| `http://localhost:5080/scalar/v1` | Documentação navegável (somente em Development) |
| `http://localhost:5080/openapi/v1.json` | Documento OpenAPI (somente em Development) |
| `http://localhost:5080/health/live` | Processo no ar |
| `http://localhost:5080/health/ready` | Pronto para atender (inclui conexão com o PostgreSQL) |

## CORS

O front chama a API direto do navegador, então a origem dele precisa estar liberada em `Cors:AllowedOrigins`. Em desenvolvimento, `appsettings.Development.json` já libera `http://localhost:5173`, a porta padrão do Vite. Para outra porta ou ambiente, ajuste a lista:

```json
{
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173", "https://app.exemplo.com.br" ]
  }
}
```

Sem a configuração, nenhuma origem é liberada. O curinga `*` não é aceito e impede a API de subir. Requisições com credenciais (cookies) ainda não são suportadas.

## Respostas de erro

Todo erro 4xx/5xx gerado pela API sai como `application/problem+json` (RFC 9457):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Recurso não encontrado.",
  "status": 404,
  "traceId": "00-033e64997bacc736c0aef759a550c880-e029f68082a32d2f-00"
}
```

- `title` vem em pt-BR. Para decidir o que fazer com o erro, use `type`/`status`, não o texto.
- `traceId` contém o `TraceId` registrado nos logs da requisição, o que permite achar o erro nos logs.
- Exceção não tratada gera `500`. Só em Development a mensagem da exceção vem em `detail`.
- Erro de validação gera `400` com `errors`, um objeto que mapeia cada campo para a lista de mensagens:

  ```json
  { "title": "Um ou mais campos são inválidos.", "status": 400, "errors": { "Nome": ["The Nome field is required."] } }
  ```

Os health checks são a exceção: respondem em texto (`Healthy`/`Unhealthy`).

## Testes

```bash
dotnet test
```

Os testes de integração criam um banco temporário `clavus_test_<data>_<guid>` e o removem no fim. Por padrão conectam com o usuário `clavus` no banco `postgres`. Para usar outro servidor ou usuário (que precisa de `CREATEDB`), defina:

```powershell
$env:CLAVUS_TEST_CONNECTION_STRING = "Host=localhost;Port=5432;Database=postgres;Username=...;Password=..."
```

## Estrutura

```
src/
  Clavus.Api/                       # host: compõe os módulos, OpenAPI, health checks, CORS, erros
  Shared/
    Clavus.Shared.Abstractions/     # contratos públicos (IModule)
    Clavus.Shared.Infrastructure/   # base de persistência (ModuleDbContext, convenções de banco)
  Modules/                          # um projeto por módulo de negócio
tests/
```

## Adicionar um módulo

1. Crie o projeto e adicione à solução:

   ```bash
   dotnet new classlib -o src/Modules/Clavus.Modules.Vendas
   dotnet sln add src/Modules/Clavus.Modules.Vendas --solution-folder src/Modules
   dotnet add src/Modules/Clavus.Modules.Vendas reference src/Shared/Clavus.Shared.Abstractions src/Shared/Clavus.Shared.Infrastructure
   dotnet add src/Clavus.Api reference src/Modules/Clavus.Modules.Vendas
   ```

   Apague o `Class1.cs` gerado e remova `TargetFramework`, `ImplicitUsings` e `Nullable` do `.csproj`, porque vêm do `Directory.Build.props`.

2. Crie o DbContext herdando `ModuleDbContext`, com schema próprio em snake_case:

   ```csharp
   public sealed class VendasDbContext(DbContextOptions<VendasDbContext> options) : ModuleDbContext(options)
   {
       public const string SchemaName = "vendas";

       protected override string Schema => SchemaName;
   }
   ```

   Os nomes de tabelas e colunas saem em snake_case e `DateTime` vira `timestamp without time zone` automaticamente. Não use `HasColumnName`/`ToTable` com maiúsculas nem `DateTime` com `Kind=Utc`.

3. Implemente `IModule` e registre o DbContext:

   ```csharp
   using Clavus.Shared.Abstractions;
   using Clavus.Shared.Infrastructure.Persistence;
   using Microsoft.AspNetCore.Builder;
   using Microsoft.AspNetCore.Http;
   using Microsoft.AspNetCore.Routing;
   using Microsoft.Extensions.Configuration;
   using Microsoft.Extensions.DependencyInjection;

   public sealed class VendasModule : IModule
   {
       public string Name => "Vendas";

       public void RegisterServices(IServiceCollection services, IConfiguration configuration)
       {
           services.AddModuleDbContext<VendasDbContext>(VendasDbContext.SchemaName);
           services.AddValidation();
       }

       public void MapEndpoints(IEndpointRouteBuilder endpoints)
       {
           var grupo = endpoints.MapGroup("/vendas").WithTags("Vendas");
           // grupo.MapGet("/pedidos", ...);
       }
   }
   ```

4. Inclua `new VendasModule()` em `src/Clavus.Api/Modules/ModuleCatalog.cs`.

5. Gere a migration com o prefixo de data `yyyyMMdd_`:

   ```bash
   dotnet ef migrations add 20261024_Inicial --project src/Modules/Clavus.Modules.Vendas --startup-project src/Clavus.Api --context VendasDbContext
   dotnet ef database update --project src/Modules/Clavus.Modules.Vendas --startup-project src/Clavus.Api --context VendasDbContext
   ```

   O histórico de migrations do módulo fica em `vendas.__ef_migrations_history`.

### Validação de entrada

A validação usa DataAnnotations nos DTOs dos endpoints e roda antes do handler. Quando falha, a resposta é `400` no formato de erro descrito acima.

```csharp
public sealed record NovoPedido([property: Required] string? Cliente, [property: Range(1, 100)] int Quantidade);

grupo.MapPost("/pedidos", (NovoPedido pedido) => ...);
```

> **Obrigatório:** chame `services.AddValidation()` no `RegisterServices` do módulo. A validação é gerada em tempo de compilação, por projeto. Sem essa chamada, os DTOs do módulo **não são validados**, e nenhum erro avisa disso.
