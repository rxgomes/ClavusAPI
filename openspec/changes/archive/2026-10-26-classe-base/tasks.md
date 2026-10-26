# Tasks

## 1. Clavus.Shared.Abstractions: classe `Entity`

- [x] 1.1 Criar `Domain/Entity.cs` (namespace `Clavus.Shared.Abstractions.Domain`): classe abstrata com `Id` (`long`, `private set`), `Identifier` (`Guid`, `private set`, inicializado com `Guid.CreateVersion7()`), `CreateIn` e `UpdateIn` (`DateTime`, `private set`) e `IsActive` (`bool`, inicializado com `true`), com XML doc em todos os membros, incluindo a limitação de `ExecuteUpdate` não atualizar `UpdateIn`. Verificar com `dotnet build` sem warnings.

## 2. Clavus.Shared.Infrastructure: convenção e preenchimento de datas

- [x] 2.1 Criar `Persistence/EntityConvention.cs` (`IModelFinalizingConvention`) que, para cada tipo raiz derivado de `Entity`, configura: chave `Id` identity; `Identifier` com default `uuidv7()` e índice único; `CreateIn`/`UpdateIn` com default `localtimestamp`; `IsActive` com default `true`; todos os defaults com `ValueGenerated = Never`. Registrar em `ModuleDbContext.ConfigureConventions`. Verificar com `dotnet build` sem warnings.
- [x] 2.2 Sobrescrever `SaveChanges(bool)` e `SaveChangesAsync(bool, CancellationToken)` no `ModuleDbContext`: `DetectChanges`, um único `DateTime.Now`; `Added` → `CreateIn` e `UpdateIn`; `Modified` → `UpdateIn` e `CreateIn` restaurado ao original e marcado como não modificado. Atualizar o XML doc da classe. Verificar com `dotnet build` sem warnings.
- [x] 2.3 Criar a entidade fictícia `Convencoes/ProdutoTeste.cs` (`sealed`, herda de `Entity`, com uma propriedade `Nome`), registrá-la no `TestModuleDbContext` e gerar a migration `20261026_EntidadeBase` com `dotnet ef migrations add 20261026_EntidadeBase --context TestModuleDbContext --output-dir Migrations/TestModule`. Verificar no arquivo gerado: tabela `produto_teste`, colunas `id`, `identifier`, `create_in`, `update_in`, `is_active`, `nome`, defaults `uuidv7()`/`localtimestamp`/`true`, índice único `ix_produto_teste_identifier` e nenhum `timestamp with time zone`.
- [x] 2.4 Criar `Convencoes/EntidadeBaseTests.cs` cobrindo os cenários de `base-entity`: colunas e tipos `not null`; `id` gerado e crescente; `identifier` versão 7 igual ao valor pré-save; `identifier` duplicado rejeitado; inclusão por SQL sem os campos preenche `identifier` (v7), `create_in`, `update_in` e `is_active = true`; `create_in == update_in` na inclusão; alteração atualiza `update_in` e preserva `create_in` (mesmo tentando alterá-lo); salvar sem alteração não muda `update_in`; `IsActive` padrão `true` e `false` persistido. Verificar que `dotnet test` passa.

## 3. Documentação e verificação final

- [x] 3.1 Atualizar o `README.md`: versão mínima PostgreSQL 18 (no lugar de 16) e seção curta sobre entidades de negócio herdarem de `Entity` (campos, defaults, `UpdateIn` automático e limitação de `ExecuteUpdate`). Verificar que o README corresponde ao comportamento testado.
- [x] 3.2 Verificação integrada: `dotnet build` sem warnings e `dotnet test` com todos os testes passando, incluindo os existentes de `database-conventions` (a nova tabela não pode usar `timestamptz`).

## Workflow follow-up

- Arquivar a change com `/opsx:archive` após revisão.
