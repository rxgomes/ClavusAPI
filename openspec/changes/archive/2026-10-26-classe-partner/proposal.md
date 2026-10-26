# Proposal

## Why

O Clavus precisa cadastrar os parceiros de negócio, ou seja, clientes e fornecedores, com documento, endereço e formas de contato. É a primeira entidade de negócio do sistema e outros cadastros e operações vão depender dela. Por isso o modelo de dados e as validações (CPF/CNPJ, CEP, telefone) precisam ficar definidos antes de existirem os endpoints.

## What Changes

- Novo módulo de negócio **`Clavus.Modules.Partners`**, o primeiro do sistema, com schema `partners`, DbContext próprio e migrations próprias. O módulo é registrado no `ModuleCatalog` do host.
- Entidade **`Partner`**, que herda de `Entity`, com:
  - `Name` (razão social ou nome) e `TradeName` (nome fantasia): string 100, obrigatórios.
  - `Document`: CPF ou CNPJ, obrigatório e único, guardado sem formatação. O tipo do documento define se o parceiro é pessoa física (CPF, 11 dígitos) ou jurídica (CNPJ, 14 caracteres). A validação inclui os dígitos verificadores e aceita o **CNPJ alfanumérico**.
  - `MunicipalRegistration` e `StateRegistration` (inscrições municipal e estadual): string 15, opcionais.
  - `Address`: endereço com CEP obrigatório (8 dígitos, sem formatação), tipo de logradouro (5), logradouro (100), número (10), bairro (100), cidade (100), estado (35) e UF (2), todos opcionais exceto o CEP.
  - `IsClient` e `IsSupplier`: default `false`.
  - Listas de `Email`, `Phone` e `Contact`.
- Entidade **`Contact`** (contato do parceiro): `Name` (100, obrigatório), `Role` (função, 30, opcional) e listas de `Email` e `Phone`.
- Entidade **`Email`**: `Address` (75, obrigatório, formato de e-mail válido) e `Type` (15, obrigatório, texto livre, como "Comercial" ou "Financeiro").
- Entidade **`Phone`**: `AreaCode` (DDD, 2 dígitos) e `Number` (8 ou 9 dígitos), ambos obrigatórios e guardados sem formatação.
- Todas as classes usam nomes em inglês. O mapeamento dos nomes pedidos está no design.
- Nova exceção de domínio compartilhada (`DomainException`) para regras violadas, com mensagens em pt-BR.

## Capabilities

### New Capabilities

- `partners`: cadastro de parceiros (clientes e fornecedores). Cobre os dados do parceiro, documento CPF/CNPJ, endereço, contatos, e-mails e telefones, com suas validações e regras de unicidade.

### Modified Capabilities

Nenhuma. As tabelas novas seguem `database-conventions` e `base-entity` sem alterar essas regras.

## Impact

- **Módulos afetados:** cria o módulo `Partners` (projeto `src/Modules/Clavus.Modules.Partners`, schema `partners`). O host `Clavus.Api` só ganha a referência ao projeto e a entrada no `ModuleCatalog`. `Clavus.Shared.Abstractions` ganha a `DomainException`.
- **Contrato da API:** nenhuma mudança. Esta change não cria endpoints nem DTOs. **Não é breaking.**
- **Impacto no front (`..\UI`):** nenhum imediato. As regras de validação definidas aqui (CPF/CNPJ alfanumérico, CEP, DDD e telefone sem formatação) serão a base dos contratos da change de endpoints, e o front poderá reaproveitá-las nas máscaras e validações de formulário.
- **Banco:** schema `partners` com as tabelas `partner`, `contact`, `email` e `phone`, criadas pela migration inicial do módulo. É preciso rodar `dotnet ef database update` no ambiente.
- **Dependências novas:** nenhum pacote novo. O novo módulo depende só de `Clavus.Shared.*`.

### Fora do escopo

- Endpoints HTTP (CRUD), DTOs, documentação OpenAPI e a conversão de `DomainException` em resposta 400. Ficam para a change de endpoints de parceiros.
- Consulta a serviços externos (Receita Federal para CNPJ, ViaCEP para endereço).
- Validar a inscrição estadual por UF e a inscrição municipal por município. Nesta change as duas são só texto com tamanho máximo.
- Exigir que o parceiro seja cliente ou fornecedor: os dois indicadores podem ficar `false`.
- Mais de um endereço por parceiro, endereço do contato e indicação de e-mail ou telefone principal.
- Aplicar as migrations automaticamente na inicialização da API.
