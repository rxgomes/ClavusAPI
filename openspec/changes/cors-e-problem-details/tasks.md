# Tasks

## 1. Clavus.Api: CORS

- [x] 1.1 Criar `Cors/CorsExtensions.cs` com `AddClavusCors(configuration)` (política padrão a partir de `Cors:AllowedOrigins`, qualquer método e cabeçalho, preflight max-age de 10 min, sem credenciais, falha na inicialização se houver `*`) e `UseClavusCors()`. Incluir `Cors:AllowedOrigins: ["http://localhost:5173"]` em `appsettings.Development.json`. Verificar com `dotnet build` sem warnings.
- [x] 1.2 Estender `ClavusApiFactory` para aceitar configurações extras (ex.: `Cors:AllowedOrigins`) e criar `CorsTests` cobrindo os cenários de `cross-origin-access`: origem permitida, origem não permitida, preflight de POST com `content-type` (204 e cabeçalhos), preflight de origem não permitida, Production sem configuração, ausência de `Access-Control-Allow-Credentials` e falha de inicialização com `*`. Verificar que `dotnet test` passa.

## 2. Clavus.Api: Problem Details e validação

- [x] 2.1 Criar `Errors/ErrorHandlingExtensions.cs` com `AddClavusErrorHandling()` (`AddProblemDetails` com títulos pt-BR por status, `detail` da exceção só em Development, título pt-BR para validação; `AddValidation()`; `RouteHandlerOptions.ThrowOnBadRequest = false`) e `UseClavusErrorHandling()` (`UseExceptionHandler` + `UseStatusCodePages`). Ligar no `Program.cs` na ordem do design (erros, CORS, endpoints). Verificar com `dotnet build` sem warnings e com `curl` em uma rota inexistente, que deve voltar `application/problem+json` com status 404.
- [x] 2.2 Adicionar à `ClavusApiFactory` o registro de endpoints de teste em `/__testes/*` (exceção, validação com DTO `[Required]`, corpo JSON e rota só GET). Validar primeiro que um endpoint simples responde. Se o `IStartupFilter` não funcionar, aplicar a alternativa do design. Verificar com um teste mínimo passando.
- [x] 2.3 Criar `ErrorResponsesTests` cobrindo os cenários de `error-responses`: campos obrigatórios e `status` igual ao HTTP, título pt-BR do 404, 500 sem detalhes em Production, 500 com `detail` em Development, 405, JSON malformado (400), validação com `errors` por campo, e health check `503`/`Unhealthy` sem `application/problem+json`. Verificar que `dotnet test` passa.
- [x] 2.4 Criar o `ILoggerProvider` de teste e o teste que confirma que o `traceId` da resposta 500 aparece no log de erro da requisição. Verificar que `dotnet test` passa.
- [x] 2.5 Confirmar que a validação nativa funciona em endpoint declarado fora do projeto Web (o teste de validação da 2.3 cobre isso). Se não funcionar, ligar a geração nos módulos (`src/Modules/Directory.Build.props`) conforme o design. Verificar com o teste de validação passando.

## 3. Documentação e verificação final

- [x] 3.1 Atualizar o README: seção de CORS (como adicionar origens em `Cors:AllowedOrigins`), formato de erro (Problem Details, campos, `traceId`) e validação de DTOs com DataAnnotations nos módulos. Verificar que os exemplos do README correspondem ao comportamento testado.
- [x] 3.2 Verificação integrada: `dotnet build` sem warnings e `dotnet test` com todos os testes passando, incluindo os 18 existentes. Verificar também manualmente, com a API rodando no perfil `http`: preflight com `curl -X OPTIONS -H "Origin: http://localhost:5173" -H "Access-Control-Request-Method: POST"` e 404 em Problem Details.

## Workflow follow-up

- Arquivar a change com `/opsx:archive` após revisão.
