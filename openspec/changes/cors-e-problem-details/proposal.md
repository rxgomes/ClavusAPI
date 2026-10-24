# Proposal

## Why

O front (`..\UI`) vai chamar a API direto do navegador, a partir de outra origem (Vite em `http://localhost:5173` em desenvolvimento). Sem CORS, o navegador bloqueia essas chamadas. Além disso, hoje cada tipo de erro sai num formato diferente: 404 com corpo vazio, 500 com página ou corpo vazio. Esta é a hora de fixar um formato único de erro, antes de existir o primeiro endpoint de negócio, para que ele não vire uma mudança de contrato depois.

## What Changes

- CORS no host `Clavus.Api`, com as origens permitidas vindas da configuração (`Cors:AllowedOrigins`). Em desenvolvimento, `http://localhost:5173`. Sem origens configuradas, nenhuma requisição cross-origin é permitida.
- Respostas de erro no formato **Problem Details (RFC 9457)**, `Content-Type: application/problem+json`, para:
  - exceções não tratadas (500), sem detalhes internos fora de Development;
  - rotas inexistentes (404) e métodos não suportados (405);
  - corpo de requisição inválido ou ilegível (400);
  - erros de validação de entrada (400, com os erros por campo).
- Todo Problem Details carrega `traceId` para correlacionar com os logs.
- Títulos das respostas de erro em pt-BR.

## Capabilities

### New Capabilities

- `cross-origin-access`: quais origens de navegador podem chamar a API e como isso é configurado por ambiente.
- `error-responses`: formato e conteúdo das respostas de erro da API, consumidas pelo front.

### Modified Capabilities

Nenhuma. O `health-check` continua respondendo `Healthy`/`Unhealthy` em texto, e um cenário da spec `error-responses` garante isso.

## Impact

- **Módulos afetados:** nenhum módulo de negócio (ainda não existem). Só o host `Clavus.Api` muda. Os módulos futuros herdam o formato de erro e a validação sem precisar configurar nada.
- **Contrato da API:** a mudança **não é breaking**, porque ainda não há endpoints de negócio. Daqui para frente, todo erro HTTP (4xx/5xx) gerado pela API vem como `application/problem+json`, com `type`, `title`, `status`, `traceId` e, quando houver, `detail`, `instance` e `errors`.
- **Impacto no front (`..\UI`):** o cliente HTTP pode tratar erros num ponto só, lendo `title`/`detail` para mensagens e `errors` (campo → mensagens) para validação de formulários. O front precisa rodar em uma das origens configuradas, por padrão `http://localhost:5173`.
- **Dependências novas:** nenhuma. Tudo usa recursos nativos do ASP.NET Core 10.

### Fora do escopo

- Autenticação/autorização e, por consequência, CORS com credenciais (cookies). Isso será revisto na change de autenticação.
- Respostas 401/403. Ainda não existem e entram com a autenticação.
- Rate limiting (429), versionamento de API e logging estruturado/observabilidade.
- Documentar as respostas de erro no OpenAPI de cada endpoint. Cada módulo faz isso nos próprios endpoints.
- Códigos de erro de negócio (`type` próprio por regra de negócio). Ficam para quando existirem regras de negócio.
