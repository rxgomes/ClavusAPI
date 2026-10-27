# Design

## Context

- O host compõe os módulos pelo `ModuleCatalog`. Cada `IModule` registra serviços (`RegisterServices`) e mapeia endpoints (`MapEndpoints`) em Minimal API. `PartnersModule.MapEndpoints` hoje está vazio.
- A validação de entrada usa a validação nativa de Minimal API do .NET 10 (`services.AddValidation()`). Ela gera validadores por compilação, então cada módulo chama `AddValidation()` no próprio `RegisterServices`, como o `ModuloDeTeste` já faz. Erros de validação saem como `HttpValidationProblemDetails` com título pt-BR (`ErrorHandlingExtensions`).
- O domínio (`Partner`, `Contact`, `Email`, `Phone`, `Address`, `Document`) normaliza e valida os valores e lança `DomainException` com mensagem pt-BR. Hoje essa exceção não tem tratamento e viraria `500`.
- `PartnersDbContext` já exclui e-mails e telefones órfãos no `SaveChanges`, e o banco tem índice único em `partner.document`.
- `Entity` expõe `Id` (`long`, identity) e `Identifier` (`Guid` v7, único).

## Goals / Non-Goals

**Goals:**
- Definir o padrão de endpoints por módulo que os próximos módulos vão copiar: classe de endpoints, DTOs, serviço de aplicação e tratamento de erros.
- Manter o domínio como única fonte das regras de formato (CPF/CNPJ, CEP etc.). Os DTOs só validam presença e tamanho.

**Non-Goals:**
- Camada genérica de repositório ou especificações de consulta. O serviço usa o `PartnersDbContext` diretamente.
- Biblioteca de mapeamento (AutoMapper etc.). O mapeamento é manual.

## Decisions

### 1. Endpoints por módulo: classe estática em `Endpoints/`, Minimal API
O módulo ganha `Endpoints/PartnerEndpoints.cs`, uma classe estática com `MapPartnerEndpoints(this IEndpointRouteBuilder)`. Ela cria `MapGroup("/partners").WithTags("Parceiros")` e declara os cinco endpoints, cada um com `WithName`, `WithSummary` em pt-BR e `Produces`/`ProducesProblem`/`ProducesValidationProblem` para o OpenAPI. Os handlers são métodos estáticos privados que retornam `Results<...>` tipados. `PartnersModule.MapEndpoints` só chama `endpoints.MapPartnerEndpoints()`.

- *Alternativa:* controllers MVC (`ControllerBase`) carregados como `ApplicationPart`. Rejeitada a pedido do usuário: o host e os testes já usam Minimal API, a validação nativa e o `IModule.MapEndpoints`. Controllers exigiriam `AddControllers`, outro pipeline de validação (`ModelState`) e outra configuração de títulos pt-BR.
- A convenção "uma classe de endpoints por recurso, em `Endpoints/` do módulo; o host não declara endpoints de negócio" é registrada em `CLAUDE.md` (seção nova **Endpoints**) e no `context` de `openspec/config.yaml` (bloco *Monolito modular*).

### 2. Rotas e chaves
| Método | Rota | Entrada | Sucesso |
|---|---|---|---|
| `POST` | `/partners` | corpo `CreatePartnerRequest` | `201` + `PartnerResponse` |
| `GET` | `/partners` | query `PartnerSearchQuery` | `200` + `PagedResult<PartnerSummaryResponse>` |
| `GET` | `/partners/details` | query `PartnerDetailsQuery` | `200` + `PartnerResponse` |
| `PUT` | `/partners/{identifier:guid}` | corpo `UpdatePartnerRequest` | `200` + `PartnerResponse` |
| `DELETE` | `/partners/{identifier:guid}` | - | `204` |

- `PUT` e `DELETE` usam o `Identifier` (UUID público) na rota, não o `Id`. O `Id` é sequencial e expõe volume, e o `Identifier` existe para ser a chave pública. O `id` continua aceito como filtro de pesquisa, como pedido.
- A consulta detalhada fica em `/partners/details` com query string, em vez de `/partners/{id}`, porque também precisa do filtro de tipo e aceita `id` **ou** `identifier`.
- O `201` não leva `Location`, porque não há uma rota de recurso único sem o filtro de tipo. O corpo já traz `id` e `identifier`.

### 3. Validação dos filtros de pesquisa
`PartnerSearchQuery` e `PartnerDetailsQuery` são records ligados com `[AsParameters]`:
- `IsClient` (`bool?`), `IsSupplier` (`bool?`), `Id` (`long?`), `Identifier` (`Guid?`), todos nullable e com `[FromQuery(Name = "<camelCase>")]`, para que o OpenAPI publique os parâmetros em camelCase.
- Só na pesquisa: `IsActive` (`bool?`, padrão `true` aplicado no serviço), `Page` (`int?`, `[Range(1, int.MaxValue)]`, padrão `1`) e `PageSize` (`int?`, `[Range(1, 100)]`, padrão `20`), com `ErrorMessage` em pt-BR. São nullable para que a ausência do parâmetro não vire erro de binding.
- As regras entre campos ficam em `IValidatableObject.Validate`, para sair no mesmo `errors` da validação nativa:
  - Tipo: válido só se `(IsClient == true) != (IsSupplier == true)` **e** nenhum dos dois foi enviado como `false`. Erro nas chaves `IsClient` e `IsSupplier`: "Informe isClient=true ou isSupplier=true, nunca os dois."
  - `id`/`identifier`: os dois informados geram erro nas chaves `Id` e `Identifier`: "Informe id ou identifier, nunca os dois." Em `PartnerDetailsQuery`, nenhum dos dois gera erro nas mesmas chaves: "Informe id ou identifier."
- **Chaves de `errors`:** seguem o padrão que a API já tinha (`EndpointsDeTeste`, chave `Nome`), ou seja, o nome da propriedade em PascalCase, inclusive nos caminhos aninhados (`Contacts[0].Emails[0].Type`). Converter todas as chaves para camelCase seria uma mudança transversal na `error-responses` e fica fora desta change.
- **Validação explícita no handler (plano B adotado):** os handlers de pesquisa sempre chamam `Validator.TryValidateObject` (atributos + `IValidatableObject`) e respondem `TypedResults.ValidationProblem(errors)`. Assim a regra vale mesmo que a validação nativa não cubra `[AsParameters]`. Se a nativa rodar antes, a resposta é idêntica. Como `Validator` só executa `IValidatableObject` quando os atributos passam, um `pageSize` inválido é reportado antes das regras de tipo.

### 4. DTOs (pasta `Contracts/`)
- Requisição: `CreatePartnerRequest` (`Name`, `TradeName`, `Document`, `MunicipalRegistration`, `StateRegistration`, `Address: AddressRequest`, `IsClient`, `IsSupplier`, `Emails: EmailRequest[]`, `Phones: PhoneRequest[]`, `Contacts: ContactRequest[]`) e `UpdatePartnerRequest`, com os mesmos campos mais `IsActive`. `ContactRequest` tem `Identifier: Guid?` opcional, `Name`, `Role`, `Emails` e `Phones`.
- `[Required]` e `[StringLength]` usam os limites das constantes do domínio (`Partner.NameMaxLength` etc.) e `ErrorMessage` em pt-BR. Listas ausentes viram lista vazia. A regra "cliente ou fornecedor" vai em `IValidatableObject` no request (chaves `isClient`/`isSupplier`).
- Resposta: `PartnerResponse` (todos os campos, `Address`, `Emails`/`Phones`/`Contacts` com `Identifier`, `IsActive`, `CreateIn`, `UpdateIn`) e `PartnerSummaryResponse` (campos da spec). `DocumentType` é serializado como string (`"Individual"`/`"Company"`) com `[JsonConverter(typeof(JsonStringEnumConverter<DocumentType>))]` na propriedade, sem mudar a serialização global do host.
- `PagedResult<T>` (`Items`, `Page`, `PageSize`, `TotalCount`) fica em `Clavus.Shared.Abstractions` (pasta `Pagination/`), com XML doc completo, porque as pesquisas dos próximos módulos vão usar o mesmo formato. Não tem regra de negócio, então cabe em Shared.

### 5. Serviço de aplicação
`Application/IPartnerService` (interface pública do módulo com XML doc, `<author>Ricardo Gomes</author>`, `<created>`/`<modified>` com a data da implementação) e `PartnerService` (`internal sealed`, scoped), registrados em `PartnersModule.RegisterServices` junto com `services.AddValidation()`. Métodos: `CreateAsync`, `SearchAsync`, `GetDetailsAsync`, `UpdateAsync`, `DeactivateAsync`. Os resultados de "não encontrado" e "documento duplicado" são valores de retorno (por exemplo, um enum de resultado ou `null`), não exceções. Os handlers traduzem esses resultados em `404`/`409`.

- **Pesquisa:** `AsNoTracking`, filtro `IsClient`/`IsSupplier` conforme o tipo, `IsActive == query.IsActive`, `Id`/`Identifier` opcionais, `OrderBy(TradeName).ThenBy(Id)`, `CountAsync` + `Skip/Take`, e projeção direta para `PartnerSummaryResponse`.
- **Detalhe:** `Include` de `Emails`, `Phones` e `Contacts` (com `Emails`/`Phones`), mais `AsSplitQuery` para evitar explosão cartesiana. Filtra pelo tipo e por `id`/`identifier`, sem filtrar `IsActive`.
- **Inclusão:** constrói `Document`, `Address`, `Partner`, `Contact`, `Email` e `Phone` pelos construtores do domínio. Qualquer `DomainException` sobe para o handler global (decisão 6).
- **Alteração:** carrega o agregado com os `Include`, aplica `Rename`, `ChangeDocument`, `ChangeRegistrations`, `ChangeAddress`, os indicadores e `IsActive`. Depois:
  - E-mails e telefones do parceiro: remove todos (`RemoveEmail`/`RemovePhone`) e adiciona os enviados. `Email` e `Phone` são imutáveis no domínio, e o `DeleteOrphanContactInfo` exclui os removidos.
  - Contatos: os enviados com `Identifier` são localizados entre os do parceiro e atualizados (`Update`, e e-mails/telefones substituídos como acima). Os sem `Identifier` são incluídos. Os não enviados são removidos (`RemoveContact`, cascata).
  - Um `Identifier` de contato que não pertence ao parceiro lança `DomainException("Contato não encontrado neste parceiro.")` antes de qualquer gravação, e vira `400`.
  - Todos os objetos de domínio são construídos antes de mexer no agregado, para que um erro de regra não deixe o contexto pela metade. De qualquer forma, nada é gravado sem `SaveChangesAsync`.
- **Inativação:** `IsActive = false` + `SaveChangesAsync`. Um parceiro já inativo é um no-op que também responde `204`.

### 6. Erros: `DomainException` → 400 no host; documento duplicado → 409 no módulo
- **Host:** novo `DomainExceptionHandler : IExceptionHandler` em `Clavus.Api/Errors`, registrado em `AddClavusErrorHandling` (`services.AddExceptionHandler<DomainExceptionHandler>()`). Ele escreve `400` via `IProblemDetailsService` com `Detail = exception.Message`. A mensagem é para o usuário, então aparece em qualquer ambiente. O `Personalizar` já preenche o título pt-BR (`Requisição inválida.`) e o `traceId`. O handler retorna `true` e não registra a exceção como erro (log `Information` no máximo), porque é entrada inválida, não falha.
- `TitulosPorStatus` ganha `409 → "Conflito."`.
- *Alternativa:* `try/catch` em cada handler. Rejeitada porque todo módulo futuro repetiria o mesmo código.
- **Documento duplicado** é regra do módulo: o serviço verifica `AnyAsync(p => p.Document == document && p.Identifier != atual)` antes de gravar e devolve "duplicado". Para a corrida entre a verificação e o `INSERT`, captura `DbUpdateException` com `PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }` e devolve o mesmo resultado. O handler responde `TypedResults.Problem(statusCode: 409, detail: "Já existe um parceiro com o documento informado.")`.
- Não encontrado: `TypedResults.Problem(statusCode: 404)`. O título pt-BR vem do `Personalizar`.

### 7. Banco
Não há alteração de schema e não há migration. As consultas usam colunas existentes de `partners.partner` (`is_client`, `is_supplier`, `is_active`, `id`, `identifier`, `trade_name`). `identifier` e `document` já têm índice único. Não vamos criar índice em `is_client`/`is_supplier` agora: a cardinalidade é baixa e o volume é pequeno, então isso fica para quando houver medição.

### 8. Dependências entre módulos
Nenhuma nova. `Clavus.Modules.Partners` continua dependendo só de `Clavus.Shared.*`. O `Npgsql` (para `PostgresException`) já chega de forma transitiva pelo provider EF de `Clavus.Shared.Infrastructure`.

## Risks / Trade-offs

- [PUT substitui as listas inteiras: um front que envia o parceiro sem as listas apaga e-mails, telefones e contatos] → O contrato no OpenAPI deixa claro que o `PUT` é substituição total. O front sempre envia o agregado que leu em `/details`.
- [E-mails e telefones são recriados a cada `PUT`, então o `identifier` deles muda] → É aceitável, porque nenhum outro recurso referencia e-mail ou telefone. Os contatos preservam o `identifier`.
- [Validação nativa pode não cobrir `IValidatableObject` em `[AsParameters]`] → Plano B da decisão 3, verificado pelos testes de integração.
- [Exclusão lógica + documento único: não dá para recadastrar um documento inativado] → O `409` indica o conflito, e o front oferece pesquisar inativos (`isActive=false`) e reativar via `PUT`.
- [Sem controle de concorrência: dois `PUT` simultâneos, o último vence] → Fora do escopo. Pode entrar depois com `xmin` como token de concorrência.

## Migration Plan

Deploy normal da API, sem migration. Rollback: reverter a versão. Como o schema não muda, não há passo de banco.
