# Proposal

## Why

O módulo `Partners` já tem o modelo de dados e as validações de parceiros (clientes e fornecedores), mas nenhum endpoint. O front (`..\UI`) precisa cadastrar, alterar, inativar e pesquisar parceiros, com telas separadas para clientes e fornecedores. Esta change expõe o CRUD de parceiros via HTTP e fixa a convenção de que cada módulo declara seus endpoints numa classe própria, para que os próximos módulos sigam o mesmo padrão.

## What Changes

- Nova classe de endpoints do módulo, **`PartnerEndpoints`**, com o grupo `/partners` (tag OpenAPI `Parceiros`). O `PartnersModule.MapEndpoints` só delega para ela. Os endpoints continuam em Minimal API, não em controllers MVC.
- Endpoints:
  - `POST /partners`: inclui um parceiro completo (dados, endereço, e-mails, telefones e contatos). Responde `201`.
  - `GET /partners`: pesquisa paginada de parceiros (resumo).
  - `GET /partners/details`: devolve um parceiro completo.
  - `PUT /partners/{identifier}`: altera o parceiro inteiro, incluindo as listas de e-mails, telefones e contatos, e permite reativá-lo.
  - `DELETE /partners/{identifier}`: **exclusão lógica**, que marca `IsActive = false`.
- Regras dos endpoints de pesquisa (`GET /partners` e `GET /partners/details`):
  - É obrigatório informar **exatamente um** filtro de tipo: `isClient=true` ou `isSupplier=true`, nunca os dois.
  - É possível pesquisar por `id` ou por `identifier`, nunca pelos dois. Em `GET /partners/details`, um dos dois é obrigatório.
- Na inclusão e na alteração, o parceiro MUST ser cliente, fornecedor ou ambos, para que apareça em alguma pesquisa.
- O nome do indicador de cliente continua `IsClient` (`isClient` na API). Não há renomeação para `IsCustomer`.
- Violações de regra de domínio (`DomainException`) passam a responder `400` em Problem Details, com a mensagem em pt-BR no `detail`, em qualquer ambiente. Documento já cadastrado responde `409`.
- Novo tipo compartilhado de resultado paginado em `Clavus.Shared.Abstractions`, para ser reutilizado pelas pesquisas dos próximos módulos.
- `CLAUDE.md` e `openspec/config.yaml` ganham a convenção de endpoints por módulo: uma classe de endpoints por recurso, na pasta `Endpoints/` do módulo, sem endpoints de negócio no host.

## Capabilities

### New Capabilities

- `partners-api`: contrato HTTP do cadastro de parceiros. Cobre inclusão, pesquisa paginada, consulta detalhada, alteração e inativação, as regras dos filtros de pesquisa (tipo, `id`/`identifier`) e os códigos de resposta.

### Modified Capabilities

- `error-responses`: adiciona a resposta `400` para violação de regra de negócio (`DomainException`), com a mensagem em pt-BR no `detail` em todos os ambientes, e a resposta `409` com título em pt-BR para conflito.

## Impact

- **Módulos afetados:**
  - `Partners`: endpoints, DTOs de requisição e resposta e serviço de aplicação. O domínio não muda, e não há migration.
  - Host `Clavus.Api`: tratamento de `DomainException` (400) e título pt-BR do status `409`.
  - `Clavus.Shared.Abstractions`: tipo de resultado paginado.
- **Contrato da API:** endpoints e DTOs novos em `/partners`. **Não é breaking**, pois não havia endpoints de negócio. A mudança no tratamento de erros só afeta `DomainException`, que nenhum endpoint lançava até agora.
- **Impacto no front (`..\UI`):**
  - As telas de clientes e de fornecedores consomem os mesmos endpoints. Cada uma envia sempre o seu filtro (`isClient=true` ou `isSupplier=true`).
  - Exclusão é lógica: o parceiro inativado some da pesquisa padrão (`isActive=true`), pode ser listado com `isActive=false` e reativado via `PUT`.
  - Erros de regra (CPF/CNPJ, CEP, DDD, UF inválidos) chegam como `400` com a mensagem pronta em `detail`. Erros de campo chegam em `errors`. Documento duplicado chega como `409`.
  - Os DTOs ficam no OpenAPI (`/openapi/v1.json`) para gerar os tipos do front.
- **Banco:** nenhuma alteração de schema e nenhuma migration.
- **Dependências novas:** nenhum pacote novo e nenhuma dependência nova entre módulos.

### Fora do escopo

- Renomear `IsClient` para `IsCustomer`.
- Controllers MVC: o padrão continua Minimal API.
- Pesquisa por texto (nome, nome fantasia, documento, cidade) e ordenação escolhida pelo cliente. A pesquisa desta change filtra por tipo, situação, `id` e `identifier`, com ordenação fixa.
- Exclusão física de parceiros pela API.
- Endpoints separados para incluir ou remover um contato, e-mail ou telefone isolado. As listas são alteradas pelo `PUT` do parceiro.
- Controle de concorrência otimista (ETag/versão) e autenticação/autorização.
- Consulta a serviços externos (Receita Federal, ViaCEP).
