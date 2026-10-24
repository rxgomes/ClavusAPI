# Design

## Context

O repositório contém apenas `CLAUDE.md` e `openspec/`; não existe código nem solução .NET. A máquina de desenvolvimento tem os SDKs .NET 8 e 10 (`10.0.402` é o mais recente). O front (`..\UI`) consome a API por HTTP/JSON e depende do contrato OpenAPI. Motivação e escopo em `proposal.md`; comportamento esperado em `specs/`.

## Goals / Non-Goals

**Goals:**
- Fixar o layout de pastas e projetos que todos os módulos vão seguir.
- Concentrar em um só lugar (Shared.Infrastructure) as convenções de banco, para que um módulo novo as herde sem configuração.
- Definir o contrato mínimo pelo qual o host descobre e compõe um módulo.

**Non-Goals:**
- Criar um módulo de exemplo dentro de `src/`. As convenções são verificadas por um DbContext de teste dentro do projeto de testes.
- Descoberta automática de módulos por reflection/assembly scanning.
- Aplicar migrations automaticamente na inicialização da API.

## Decisions

### 1. Layout da solução

```
Clavus.slnx
global.json                         # fixa SDK 10.0.x (rollForward: latestFeature)
Directory.Build.props               # net10.0, Nullable, ImplicitUsings, TreatWarningsAsErrors, GenerateDocumentationFile
Directory.Packages.props            # ManagePackageVersionsCentrally
.editorconfig                       # chaves obrigatórias (IDE0011), nomenclatura do CLAUDE.md
docker-compose.yml
src/
  Clavus.Api/
  Shared/
    Clavus.Shared.Abstractions/
    Clavus.Shared.Infrastructure/
  Modules/                          # vazio por enquanto (.gitkeep)
tests/
  Clavus.Api.Tests/
  Clavus.Shared.Infrastructure.Tests/
```

Dependências entre projetos: `Clavus.Api → Shared.Abstractions, Shared.Infrastructure`; `Shared.Infrastructure → Shared.Abstractions`. Módulos futuros (`Clavus.Modules.<Modulo>`) referenciam apenas os projetos `Shared.*`; o host referencia os módulos. Nenhuma dependência entre módulos é criada nesta change.

`GenerateDocumentationFile` fica ligado para que os `<summary>` exigidos pelo `CLAUDE.md` sejam validados pelo compilador (CS1591 só nos projetos que expõem interfaces públicas, para não poluir o host e os testes).

- **`.slnx` em vez de `.sln`:** formato padrão do SDK 10, legível e com menos conflitos de merge. Rider e VS atuais suportam.
- **Gerenciamento central de pacotes:** com vários módulos, garante uma versão única de EF Core/Npgsql em toda a solução.

### 2. Contrato de módulo (`Clavus.Shared.Abstractions`)

```csharp
public interface IModule
{
    string Name { get; }                                                         // ex.: "Vendas"
    void RegisterServices(IServiceCollection services, IConfiguration configuration);
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
```

O host mantém uma lista explícita de módulos (`ModuleCatalog`) em `Program.cs` e chama `RegisterServices` e `MapEndpoints` para cada um. As interfaces seguem o padrão de XML doc do `CLAUDE.md` (`<author>Ricardo Gomes</author>`, `<created>`/`<modified>` com a data da implementação).

- **Alternativa considerada:** scanning de assemblies por `IModule`. Foi descartada porque é implícita, sensível à ordem de carregamento de assemblies e difícil de depurar. Com a lista explícita, ligar ou desligar um módulo é uma linha de código.
- **Por que só `IServiceCollection`/`IEndpointRouteBuilder`:** são contratos estáveis do ASP.NET, e assim o módulo não depende do `WebApplication` do host. Exige `FrameworkReference Microsoft.AspNetCore.App` em Shared.Abstractions.

### 3. Base de persistência (`Clavus.Shared.Infrastructure`)

- **`ModuleDbContext` (abstrata)** herda de `DbContext` e expõe `protected abstract string Schema`.
  - `OnModelCreating`: `HasDefaultSchema(Schema)` e depois `base`/configurações do módulo via `ApplyConfigurationsFromAssembly` do assembly concreto.
  - `ConfigureConventions`: `Properties<DateTime>().HaveColumnType("timestamp without time zone")` e o mesmo para `DateTime?`. `DateOnly` já mapeia para `date` no Npgsql.
- **`AddClavusDatabase(configuration)`** registra um único `NpgsqlDataSource` (singleton) a partir de `ConnectionStrings:Clavus`, porque há um só banco para todo o sistema. Os DbContexts dos módulos e o health check usam essa instância, então existe um só pool de conexões. Sem a connection string, a inicialização falha com mensagem clara.
- **Extensão `AddModuleDbContext<TContext>(services, schema)`** centraliza o registro do DbContext do módulo. A configuração fica em `ConfigureModuleDbContext`, reutilizada por design-time factories e testes:
  - `UseNpgsql(dataSource, o => o.MigrationsHistoryTable("__ef_migrations_history", schema))`
  - `UseSnakeCaseNamingConvention()`
- **Colunas do histórico de migrations:** o `EFCore.NamingConventions` não renomeia as colunas da tabela de histórico (`MigrationId`, `ProductVersion`). Um `SnakeCaseHistoryRepository`, que herda `NpgsqlHistoryRepository` e sobrescreve `ConfigureTable`, mapeia essas colunas para `migration_id` e `product_version`. Ele é registrado com `ReplaceService<IHistoryRepository, ...>`. Assim a regra "consultável sem aspas" vale também para o histórico. O nome da tabela continua com o prefixo `__`, que é minúsculo e válido sem aspas.
- **Por que mapear o tipo e não usar `Npgsql.EnableLegacyTimestampBehavior`:** o switch legado é global e está deprecado. A convenção por `DbContext` é explícita e exigida pelo `CLAUDE.md`. Consequência: `DateTime` com `Kind=Utc` gravado em coluna `timestamp` gera erro no Npgsql 6+. O padrão do sistema passa a ser `DateTime` com `Kind=Unspecified`/`Local`, e isso fica documentado no XML doc da classe base.

Nesta change não há schema de negócio nem migration em `src/`. O projeto de testes define um `TestModuleDbContext` (schema `teste_convencoes`) com uma entidade `PedidoItem` (`Id`, `DataCriacao: DateTime`, `DataEntrega: DateOnly`, `DataCancelamento: DateTime?`), além de uma segunda classe com schema `teste_convencoes_b`. Os testes criam esses schemas com `Database.MigrateAsync()` a partir de uma migration de teste (`20261024_Inicial`) e inspecionam `information_schema`.

### 4. Host (`Clavus.Api`)

`Program.cs` faz apenas composição:
1. `builder.Services.AddOpenApi()`; em Development, `app.MapOpenApi()` e `app.MapScalarApiReference()`.
2. Health checks: `AddHealthChecks()` com um check `self` (tag `live`) e um `postgres` (tag `ready`). O `postgres` é implementado localmente com `NpgsqlDataSource` executando `select 1`.
   - `MapHealthChecks("/health/live", Predicate = tag live)` e `MapHealthChecks("/health/ready", Predicate = tag ready)`, com o response writer padrão (texto `Healthy`/`Unhealthy`, sem detalhes).
   - Os endpoints de health check não são endpoints de Minimal API com metadados OpenAPI, então não entram no documento. Um teste garante isso.
3. Loop sobre `ModuleCatalog` (vazio nesta change).

`appsettings.Development.json` aponta `ConnectionStrings:Clavus` para `localhost:5432`, banco `clavus`, usuário `clavus`. Funciona igual com o PostgreSQL local ou com o docker-compose. `Program` é exposto como `public partial class Program` para o `WebApplicationFactory`.

- **Check de PostgreSQL próprio em vez de `AspNetCore.HealthChecks.NpgSql`:** são cerca de 15 linhas e evitam uma dependência de terceiros só para um `select 1`.
- **Scalar em vez de Swagger UI:** o .NET 9+ removeu o Swashbuckle do template. O Scalar consome nativamente o documento de `Microsoft.AspNetCore.OpenApi`.

### 5. PostgreSQL local

O caminho principal é um PostgreSQL instalado na máquina (a máquina de desenvolvimento não tem Docker e já roda o PostgreSQL 18 como serviço). O README traz o SQL para criar o usuário `clavus`/`clavus` com `CREATEDB` e o banco `clavus` (credenciais apenas de desenvolvimento).

`docker-compose.yml` com `postgres:18` e as mesmas credenciais, porta `5432` e volume nomeado, fica no repositório como alternativa equivalente para quem tem Docker.

- **Alternativa considerada:** Testcontainers. Foi descartado porque exige Docker, que não está disponível no ambiente de desenvolvimento.

### 6. Testes

- **xUnit v3** com `Microsoft.NET.Test.Sdk`. Em `Clavus.Api.Tests`, também `Microsoft.AspNetCore.Mvc.Testing`. Os testes que precisam de banco usam uma fixture compartilhada (`PostgresFixture`) que:
  - lê a connection string de administração da variável de ambiente `CLAVUS_TEST_CONNECTION_STRING`; o padrão é `Host=localhost;Port=5432;Database=postgres;Username=clavus;Password=clavus`;
  - cria um banco temporário `clavus_test_<guid>` no início e o remove (`drop database ... with (force)`) no fim;
  - falha com mensagem clara se o servidor não estiver acessível.
  O banco temporário garante isolamento sem tocar no banco `clavus` de desenvolvimento.
- `Clavus.Shared.Infrastructure.Tests` cobre os cenários de `database-conventions`.
- `Clavus.Api.Tests` cobre `health-check` (inclusive banco inacessível, com uma connection string para uma porta sem servidor) e `api-documentation` (ambientes `Development` e `Production`).

## Risks / Trade-offs

- **[Npgsql rejeita `DateTime` UTC em coluna `timestamp`]** → Documentar a regra na classe base e cobrir com teste de ida e volta. Se surgir necessidade real de instantes UTC, abrir uma change específica.
- **[Testes de integração exigem um PostgreSQL acessível]** → Documentar no README a criação do usuário `clavus` e a variável `CLAVUS_TEST_CONNECTION_STRING`. Cada execução usa um banco temporário próprio, criado e removido pela fixture.
- **[Banco temporário pode sobrar se a execução for interrompida]** → Prefixo `clavus_test_` identifica e permite limpar manualmente. A fixture também remove bancos `clavus_test_*` com mais de 1 dia ao iniciar.
- **[`TreatWarningsAsErrors` pode travar o desenvolvimento inicial]** → Ligado desde o começo porque é mais barato do que limpar warnings depois. Ajustes pontuais via `NoWarn` documentados no `Directory.Build.props`.
- **[Lista explícita de módulos exige editar o host a cada módulo novo]** → É um trade-off aceito em troca de previsibilidade; trata-se de uma linha por módulo.
- **[Pacotes .NET 10 (EFCore.NamingConventions, Npgsql) podem ainda não ter versão estável alinhada]** → Na implementação, usar a última versão estável compatível com EF Core 10 e registrá-la em `Directory.Packages.props`.

## Migration Plan

Não há sistema em produção. A implantação é o merge na `main`. Rollback: reverter o commit. Nenhuma migration de banco real é criada.
