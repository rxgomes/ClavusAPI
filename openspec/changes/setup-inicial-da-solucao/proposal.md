# Proposal

## Why

O repositório da Clavus API ainda não tem código. Antes de implementar qualquer módulo de negócio, é preciso uma fundação que já aplique as regras do monolito modular e os padrões de banco (snake_case, datas sem timezone). Assim, cada módulo novo só precisa se encaixar na estrutura, sem reinventar host, persistência ou convenções.

## What Changes

- Criação da solução `Clavus.slnx` com build centralizado (`Directory.Build.props`) e versões de pacotes centralizadas (`Directory.Packages.props`).
- Projeto host `Clavus.Api` (ASP.NET Core, Minimal APIs) responsável apenas por compor módulos, registrar serviços, middlewares e endpoints transversais.
- Projeto `Clavus.Shared.Abstractions` com o contrato público que todo módulo implementa para ser registrado pelo host.
- Projeto `Clavus.Shared.Infrastructure` com a base de persistência que todo módulo herda: DbContext base com nomes em snake_case, `DateTime` mapeado para `timestamp without time zone` por convenção e histórico de migrations isolado por schema.
- Endpoints de health check (liveness e readiness, esta incluindo a conectividade com o PostgreSQL).
- Documento OpenAPI exposto pelo host, com interface de navegação em ambiente de desenvolvimento.
- Configuração de PostgreSQL para desenvolvimento local: usuário `clavus` em um PostgreSQL instalado na máquina, com `docker-compose.yml` opcional para quem usa Docker.
- Projetos de testes para a infraestrutura compartilhada e para o host.

## Capabilities

### New Capabilities

- `health-check`: endpoints que informam se a API está no ar e se está pronta para atender (dependências como o banco acessíveis).
- `api-documentation`: publicação do contrato HTTP da API em formato OpenAPI, consumido pelo front.
- `database-conventions`: garantias sobre como os objetos de banco de todos os módulos são criados (nomes em snake_case consultáveis sem aspas, datas sem timezone, schema e histórico de migrations por módulo).

### Modified Capabilities

Nenhuma.

## Impact

- **Módulos afetados:** nenhum módulo de negócio (ainda não existem). São criados apenas o host `Clavus.Api` e os projetos `Clavus.Shared.Abstractions` e `Clavus.Shared.Infrastructure`.
- **Contrato da API:** não há contrato anterior, então não há breaking change. Surgem os endpoints `/health/live`, `/health/ready` e o documento `/openapi/v1.json`. Impacto no front (`..\UI`): nenhum obrigatório; o front pode passar a gerar tipos a partir do documento OpenAPI.
- **Dependências novas:** Npgsql/EF Core (`Npgsql.EntityFrameworkCore.PostgreSQL`), `EFCore.NamingConventions`, `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore` (UI do OpenAPI em desenvolvimento), xUnit e `Microsoft.AspNetCore.Mvc.Testing` nos testes.
- **Ambiente:** desenvolvimento e testes de integração passam a exigir um PostgreSQL acessível (local ou via Docker) com o usuário `clavus`.

### Fora do escopo

- Qualquer módulo de negócio, entidade ou migration de negócio.
- Autenticação e autorização (change posterior).
- CORS para o front, versionamento de API, logging estruturado, observabilidade (métricas/tracing), tratamento global de erros (ProblemDetails customizado) e pipeline de CI/CD.
- Dockerfile/deploy da própria API.
