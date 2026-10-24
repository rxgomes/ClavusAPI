# Tasks

## 1. Solução e build

- [x] 1.1 Criar `global.json` (SDK 10.0.x, `rollForward: latestFeature`), `Directory.Build.props` (net10.0, Nullable, ImplicitUsings, TreatWarningsAsErrors) e `Directory.Packages.props` (gerenciamento central). Verificar com `dotnet --version` na raiz, que deve retornar 10.0.x.
- [x] 1.2 Criar `.editorconfig` com chaves obrigatórias (IDE0011 como erro no build) e regras de nomenclatura do `CLAUDE.md` (campos privados `_camelCase`, interfaces com prefixo `I`, `Async` em métodos assíncronos). Verificar que um `if` sem chaves gera erro em `dotnet build`.
- [x] 1.3 Criar `Clavus.slnx` com as pastas de solução `src/Shared`, `src/Modules` e `tests`, e `src/Modules/.gitkeep`. Verificar com `dotnet sln list`.
- [x] 1.4 Criar `docker-compose.yml` (opcional) com `postgres:18` (banco/usuário/senha `clavus`, porta 5432, volume nomeado) e o script `scripts/criar-usuario-dev.sql` (usuário `clavus` com `CREATEDB` e banco `clavus`) para o PostgreSQL local. Verificar com `docker compose config`, que deve validar o arquivo, e conectando com `clavus`/`clavus` no PostgreSQL local depois de executar o script.

## 2. Clavus.Shared.Abstractions

- [x] 2.1 Criar o projeto (classlib, `FrameworkReference Microsoft.AspNetCore.App`, `GenerateDocumentationFile`) e adicioná-lo à solução. Verificar com `dotnet build`.
- [x] 2.2 Criar a interface `IModule` (`Name`, `RegisterServices`, `MapEndpoints`) com XML doc completo (`<summary>`, `<param>`, `<author>`, `<created>`, `<modified>`). Verificar que `dotnet build` passa sem CS1591.

## 3. Clavus.Shared.Infrastructure

- [x] 3.1 Criar o projeto com referência a Shared.Abstractions e os pacotes `Npgsql.EntityFrameworkCore.PostgreSQL` e `EFCore.NamingConventions` (versões estáveis para EF Core 10 em `Directory.Packages.props`). Verificar com `dotnet build`.
- [x] 3.2 Implementar `ModuleDbContext` abstrato (schema padrão do módulo; `ConfigureConventions` mapeando `DateTime`/`DateTime?` para `timestamp without time zone`) com XML doc explicando a regra de `DateTime` sem `Kind=Utc`. Verificar com `dotnet build`.
- [x] 3.3 Implementar `SnakeCaseHistoryRepository` (colunas `migration_id` e `product_version`) e a extensão `AddModuleDbContext<TContext>` (connection string `ConnectionStrings:Clavus`, `UseSnakeCaseNamingConvention`, `MigrationsHistoryTable("__ef_migrations_history", schema)`, `ReplaceService` do histórico). Interfaces públicas novas devem ter XML doc completo. Verificar com `dotnet build`.
- [x] 3.4 Criar `tests/Clavus.Shared.Infrastructure.Tests` (xUnit v3) com `PostgresFixture` (banco temporário `clavus_test_<guid>` a partir de `CLAVUS_TEST_CONNECTION_STRING`), `TestModuleDbContext` (schema `teste_convencoes`, entidade `PedidoItem`), um segundo contexto (schema `teste_convencoes_b`) e as migrations de teste `20261024_Inicial`. Verificar com `dotnet test`, com projeto compilando e fixture criando e removendo o banco temporário.
- [x] 3.5 Escrever testes para os cenários de `database-conventions`: tabela/colunas em snake_case consultáveis sem aspas, `DateTime`/`DateTime?` → `timestamp without time zone`, `DateOnly` → `date`, ida e volta de `DateTime` sem alteração, tabelas no schema do módulo, `__ef_migrations_history` por schema com colunas snake_case. Verificar que `dotnet test` passa.

## 4. Clavus.Api (host)

- [x] 4.1 Criar o projeto `Clavus.Api` (`web`, Minimal APIs) com referências aos projetos Shared, `ModuleCatalog` vazio e loop de `RegisterServices`/`MapEndpoints`, além de `public partial class Program`. Verificar que `dotnet run` sobe a API.
- [x] 4.2 Configurar OpenAPI (`AddOpenApi` com título `Clavus API`) e Scalar (`/scalar/v1`), mapeados apenas em `Development`. Verificar manualmente que `/openapi/v1.json` e `/scalar/v1` respondem em Development.
- [x] 4.3 Implementar o health check de PostgreSQL (`select 1` via `NpgsqlDataSource`, tag `ready`) e o check `self` (tag `live`), mapeando `/health/live` e `/health/ready` com o writer padrão. Configurar `ConnectionStrings:Clavus` em `appsettings.Development.json` apontando para `localhost:5432`/`clavus`. Verificar com `curl` em ambos os endpoints, com o banco acessível e com uma connection string inválida.
- [x] 4.4 Criar `tests/Clavus.Api.Tests` (xUnit v3, Mvc.Testing, reutilizando o padrão de `PostgresFixture`) com testes dos cenários de `health-check` (live 200 sempre; ready 200 com banco e 503 sem banco; corpo sem detalhes de erro) e de `api-documentation` (200 em Development com título `Clavus API`; 404 em Production; `/scalar/v1` 200 em Development; documento sem `/health/*`). Verificar que `dotnet test` passa.

## 5. Documentação e verificação final

- [x] 5.1 Criar `README.md` com pré-requisitos (.NET 10 SDK, PostgreSQL local ou Docker), como preparar o banco (script do usuário `clavus` ou `docker compose up -d`), a variável `CLAVUS_TEST_CONNECTION_STRING`, rodar a API, abrir a documentação, rodar os testes e o passo a passo para adicionar um módulo (`Clavus.Modules.<Modulo>`, herdar `ModuleDbContext`, registrar no `ModuleCatalog`, migration com prefixo `yyyyMMdd_`). Verificar que os comandos do README funcionam como escritos.
- [x] 5.2 Verificação integrada: `dotnet build` sem warnings e `dotnet test` com todos os testes passando na solução inteira, partindo de um clone limpo.

## Workflow follow-up

- Arquivar a change com `/opsx:archive` após revisão.
