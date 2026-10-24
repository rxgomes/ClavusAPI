# Design

## Context

O `Program.cs` do host hoje só registra OpenAPI, o banco, os health checks e o loop de módulos (`ModuleCatalog`). Não há CORS, `UseExceptionHandler` nem `UseStatusCodePages`. Por isso um 404 sai com corpo vazio e uma exceção sai como 500 sem corpo, ou com a página de desenvolvedor. Os testes de integração usam `ClavusApiFactory` (`WebApplicationFactory<Program>`) com ambiente e connection string configuráveis. Os comportamentos esperados estão nas specs `cross-origin-access` e `error-responses`.

## Goals / Non-Goals

**Goals:**
- Configurar CORS e o pipeline de erros num só lugar do host, de modo que os módulos não precisem fazer nada.
- Usar só recursos nativos do ASP.NET Core 10, sem bibliotecas de terceiros.

**Non-Goals:**
- Exceções de domínio mapeadas para status específicos, como `NotFoundException` → 404. Isso fica para quando existirem regras de negócio.
- Biblioteca de validação (FluentValidation). A validação nativa de Minimal APIs do .NET 10 cobre o necessário agora.

## Decisions

### 1. Organização no host

Duas extensões novas, seguindo o padrão de `HealthChecks/HealthCheckExtensions.cs`:

- `Cors/CorsExtensions.cs`: `AddClavusCors(configuration)` / `UseClavusCors()`
- `Errors/ErrorHandlingExtensions.cs`: `AddClavusErrorHandling()` / `UseClavusErrorHandling()`

Ordem do pipeline no `Program.cs`:

```
UseClavusErrorHandling()   // UseExceptionHandler + UseStatusCodePages: primeiro, para capturar tudo
UseClavusCors()            // antes dos endpoints, para preflight e cabeçalhos
Map... (OpenAPI, health, módulos)
```

O `UseExceptionHandler` fica antes do CORS. Assim as respostas de erro de origens permitidas também levam `Access-Control-Allow-Origin`, e o front consegue ler o corpo do erro.

### 2. CORS

- Uma política padrão (`AddDefaultPolicy`) com `WithOrigins(Cors:AllowedOrigins)`, `AllowAnyMethod()`, `AllowAnyHeader()` e `SetPreflightMaxAge(10 min)`. Não há `AllowCredentials()`.
- `Cors:AllowedOrigins` é um array em `appsettings`. `appsettings.Development.json` traz `["http://localhost:5173"]`. `appsettings.json` não define o valor: a lista vazia faz a política não liberar nenhuma origem.
- Se a configuração tiver `*`, a inicialização falha com mensagem clara. O curinga é proibido pela spec, e falhar cedo é melhor do que ignorar a entrada em silêncio.
- **Alternativa considerada:** CORS por endpoint ou por grupo de módulo. Foi descartada porque o front é um só e consome a API inteira, então uma política global basta.

### 3. Problem Details

- `AddProblemDetails(o => o.CustomizeProblemDetails = ...)` centraliza:
  - **`title` em pt-BR** por status: 400 `Requisição inválida.`, 404 `Recurso não encontrado.`, 405 `Método não permitido.`, 500 `Ocorreu um erro inesperado.`. Os outros status mantêm o título padrão do ASP.NET. Quem já definiu o título (validação) não é sobrescrito.
  - **`traceId`:** o writer padrão já adiciona `Activity.Current?.Id ?? HttpContext.TraceIdentifier`. Com o Activity do ASP.NET, esse é o mesmo trace dos logs (escopo `TraceId`/`SpanId`). Só preciso garantir isso por teste.
  - **Exceções:** com `IExceptionHandlerFeature` disponível, em `Development` preenche `detail` com a mensagem da exceção. Fora de Development não adiciona nada.
- `UseExceptionHandler()` sem caminho usa o `IProblemDetailsService` e já registra a exceção no log (`Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware`, nível Error) dentro do escopo da requisição. `UseDeveloperExceptionPage` deixa de ser usado: em Development, a resposta também é Problem Details, só que com `detail`.
- `UseStatusCodePages()` gera Problem Details para respostas 4xx/5xx **sem corpo**, como 404 e 405 de roteamento. Respostas que já têm corpo não são alteradas. É por isso que os health checks, que escrevem `Unhealthy` com status 503, continuam como estão.
- **`BadHttpRequestException`** (JSON malformado, binding inválido): em Development, as Minimal APIs lançam e o handler responde 400. Configuro `RouteHandlerOptions.ThrowOnBadRequest = false` em todos os ambientes, para que o framework devolva 400 sem corpo e o `UseStatusCodePages` o converta em Problem Details. Assim o comportamento é igual nos dois ambientes.

### 4. Validação

- `builder.Services.AddValidation()` é a validação nativa de Minimal APIs do .NET 10, via source generator com DataAnnotations. Parâmetros e DTOs com `[Required]`, `[StringLength]` etc. são validados automaticamente antes do handler e geram `HttpValidationProblemDetails` (400, `errors`).
- O `title` de validação é definido em pt-BR (`Um ou mais campos são inválidos.`) no `CustomizeProblemDetails` quando o problema for `HttpValidationProblemDetails`.
- **Cada módulo chama `services.AddValidation()` no próprio `RegisterServices`.** Isso foi confirmado na implementação: o source generator só gera o resolver de tipos no projeto que chama `AddValidation()`, e cobre apenas os tipos usados nos endpoints daquela compilação. A chamada no host não enxerga os DTOs dos módulos. Cada chamada adiciona um resolver, e eles se somam. A chamada no host continua, para registrar a infraestrutura de validação mesmo sem módulos. O README documenta isso.
- **Alternativa considerada:** FluentValidation. Foi descartada por ser dependência nova sem necessidade concreta. Pode ser revista se surgirem regras de validação complexas.

### 5. Testes

Os testes de `error-responses` precisam de endpoints que lancem exceção, validem entrada e aceitem só GET. Como não existe módulo de negócio, `ClavusApiFactory` registra um `ModuloDeTeste` (`IModule`) com rotas em `/__testes/*`. Para isso, o host passa a registrar os módulos do `ModuleCatalog` como `IModule` no container e mapeia os endpoints a partir de `app.Services.GetServices<IModule>()`. Assim o teste registra o módulo dele no DI e passa pelo mesmo pipeline dos módulos reais. Nenhum endpoint de teste entra no código de produção.

A primeira tentativa foi um `IStartupFilter`, e ela não funcionou: o route builder global da `WebApplication` (`__GlobalEndpointRouteBuilder`) deixa de estar disponível depois que o pipeline é montado.

A verificação do `traceId` nos logs usa um `ILoggerProvider` de teste que guarda as mensagens e os escopos, sem pacote novo.

Os testes de CORS enviam `Origin` e preflight `OPTIONS` pelo `HttpClient` da factory e conferem os cabeçalhos, em Development e em Production.

## Risks / Trade-offs

- **[Módulo que esquecer `services.AddValidation()` não valida a entrada]** → Isso é silencioso: os DTOs são aceitos sem validação. O README destaca a chamada como obrigatória. Um teste por módulo cobrindo um caso inválido detecta o esquecimento.
- **[`ThrowOnBadRequest = false` esconde o detalhe do erro de binding em Development]** → Isso é aceitável: o 400 continua no formato padrão, e o log de Debug do framework registra a causa.
- **[Títulos em pt-BR divergem dos títulos padrão da RFC]** → A RFC 9457 permite título legível em qualquer idioma. O `type` continua sendo a URI padrão por status, e é por ele que o front deve se orientar para lógica.
- **[Front em porta diferente de 5173]** → Basta incluir a origem em `Cors:AllowedOrigins`, sem mudar código.

## Migration Plan

Não há banco envolvido nesta change: sem schema, tabela ou migration. Deploy é o merge na `main`. Rollback: reverter o commit.
