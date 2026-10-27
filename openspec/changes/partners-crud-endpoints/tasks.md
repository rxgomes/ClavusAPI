# Tasks

## 1. Convenções (CLAUDE.md e OpenSpec)

- [x] 1.1 Adicionar a seção **Endpoints** ao `CLAUDE.md`: uma classe estática de endpoints por recurso em `Endpoints/` do módulo (`Map<Recurso>Endpoints`, `MapGroup("/<recurso>")`, `WithTags` em pt-BR, `WithSummary` e metadados de resposta). Incluir também que `IModule.MapEndpoints` só delega, que o host não declara endpoints de negócio, que os DTOs ficam em `Contracts/` e que cada módulo chama `services.AddValidation()`. Verificar relendo o arquivo.
- [x] 1.2 Atualizar o bloco *Monolito modular* do `context` em `openspec/config.yaml` com a mesma convenção, de forma resumida, e verificar com `openspec context --json` que o YAML continua válido.

## 2. Shared (`Clavus.Shared.Abstractions`)

- [x] 2.1 Criar `Pagination/PagedResult<T>` (`Items`, `Page`, `PageSize`, `TotalCount`) com XML doc em todos os membros públicos e verificar com `dotnet build` sem warnings (o CS1591 vale em Shared).

## 3. Host (`Clavus.Api`): tratamento de erros

- [x] 3.1 Criar `Errors/DomainExceptionHandler` (`IExceptionHandler`) que responde `400` em Problem Details com `detail` = mensagem da `DomainException` em qualquer ambiente, sem logar como erro. Registrar em `AddClavusErrorHandling`.
- [x] 3.2 Adicionar `409 → "Conflito."` em `TitulosPorStatus`.
- [ ] 3.3 Em `EndpointsDeTeste`, adicionar `/__testes/regra` (lança `DomainException("CPF inválido.")`) e `/__testes/conflito` (`TypedResults.Problem(statusCode: 409)`). Em `ErrorResponsesTests`, cobrir os cenários da delta `error-responses`: `400` com `title`/`detail` em `Production` e sem stack trace, `500` inalterado para `InvalidOperationException` e `409` com `title` `Conflito.`. Verificar com `dotnet test tests/Clavus.Api.Tests`.

## 4. Módulo Partners: contratos e serviço

- [x] 4.1 Criar em `Contracts/` os DTOs de requisição (`CreatePartnerRequest`, `UpdatePartnerRequest`, `AddressRequest`, `ContactRequest`, `EmailRequest`, `PhoneRequest`) com `[Required]`/`[StringLength]` usando as constantes do domínio e `ErrorMessage` em pt-BR. A regra "cliente ou fornecedor" vai em `IValidatableObject` (chaves `isClient`/`isSupplier`). Verificar com `dotnet build`.
- [x] 4.2 Criar `PartnerSearchQuery` e `PartnerDetailsQuery` (com `[AsParameters]`), com `Range` de `page`/`pageSize` e as regras de tipo e de `id`/`identifier` em `IValidatableObject`, mensagens em pt-BR. Criar também os DTOs de resposta `PartnerResponse` e `PartnerSummaryResponse`, com `documentType` serializado como string. Verificar com `dotnet build`.
- [x] 4.3 Criar `Application/IPartnerService` (XML doc com `<author>Ricardo Gomes</author>`, `<created>`/`<modified>` da data da implementação) e `PartnerService` com `CreateAsync`, `SearchAsync`, `GetDetailsAsync`, `UpdateAsync` e `DeactivateAsync`, conforme a decisão 5 do design. O documento duplicado é tratado com verificação prévia e captura de `UniqueViolation`. Verificar com `dotnet build`.
- [x] 4.4 Em `PartnersModule.RegisterServices`, registrar `IPartnerService` (scoped) e chamar `services.AddValidation()`. Verificar com `dotnet build`.

## 5. Módulo Partners: endpoints

- [x] 5.1 Criar `Endpoints/PartnerEndpoints` com `MapPartnerEndpoints()`: grupo `/partners`, tag `Parceiros`, endpoints `POST /`, `GET /`, `GET /details`, `PUT /{identifier:guid}` e `DELETE /{identifier:guid}`, com `WithName`, `WithSummary` em pt-BR e `Produces`/`ProducesProblem`/`ProducesValidationProblem` (400/404/409). Os handlers traduzem os resultados do serviço em `201`/`200`/`204`/`404`/`409`. `PartnersModule.MapEndpoints` só delega. Verificar com `dotnet build` e com o endpoint visível em `/scalar/v1` ao rodar a API em `Development`.
- [ ] 5.2 Criar a infraestrutura de teste de integração dos endpoints em `tests/Clavus.Api.Tests/Partners/`: `ClavusApiFactory` sobre o `PostgresFixture`, `MigrateAsync` do `PartnersDbContext` e helpers para montar requisições válidas. Verificar com um teste de fumaça `POST` → `201`.
- [ ] 5.3 Testes de inclusão: inclusão completa, normalização, nenhum indicador (`400` com `isClient`/`isSupplier`), cliente e fornecedor, campo ausente (`tradeName`), e-mail de contato sem tipo, CPF inválido (`400` + `detail`) e documento duplicado formatado de outro jeito (`409`). Verificar com `dotnet test`.
- [ ] 5.4 Testes de pesquisa (`GET /partners`): filtro de tipo (só cliente, os dois, nenhum, `false`), `id`+`identifier` juntos (`400`), pesquisa por `identifier`, `id` de outro tipo (vazio), paginação (25 registros, página 2) e `pageSize=101`, situação (`isActive` padrão e `false`). Se `IValidatableObject` não rodar em `[AsParameters]`, aplicar o plano B do design (validação no handler + `ValidationProblem`) até os testes passarem. Verificar com `dotnet test`.
- [ ] 5.5 Testes de detalhe (`GET /partners/details`): por `id`, sem `id`/`identifier` (`400`), parceiro de outro tipo (`404`), inexistente (`404`) e parceiro inativo devolvido. Verificar com `dotnet test`.
- [ ] 5.6 Testes de alteração (`PUT`): reativar, inexistente (`404`), UF inválida (`400` e nada alterado), documento de outro parceiro (`409`), remover e-mail mantendo o contato (mesmo `identifier`, nome novo), remover contato (cascata no banco) e contato de outro parceiro (`400`, nada alterado). Verificar com `dotnet test`.
- [ ] 5.7 Testes de inativação (`DELETE`): inativar (`204`, detalhe com `isActive: false` e listas preservadas), inativar de novo (`204`) e inexistente (`404`). Verificar com `dotnet test`.
- [ ] 5.8 Em `ApiDocumentationTests`, verificar que `/openapi/v1.json` contém `/partners`, `/partners/details` e `/partners/{identifier}` sob a tag `Parceiros`. Verificar com `dotnet test tests/Clavus.Api.Tests`.

## 6. Verificação integrada

- [ ] 6.1 Rodar `dotnet build` (warnings como erro) e `dotnet test` na solução inteira e confirmar que tudo passa, inclusive os testes de domínio e de persistência existentes do módulo Partners.
- [x] 6.2 Rodar `openspec validate partners-crud-endpoints --strict` e confirmar que passa.

## Workflow follow-up

- Sem migration nesta change: o schema `partners` não muda.
- Arquivar a change (`/opsx:archive`) depois da revisão. Isso cria `openspec/specs/partners-api/spec.md` e incorpora a delta de `error-responses`.
