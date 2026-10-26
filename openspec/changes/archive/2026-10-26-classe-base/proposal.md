# Proposal

## Why

Toda tabela de negócio do Clavus vai precisar dos mesmos campos de controle: chave numérica, identificador público, datas de criação e alteração e um indicador de ativo. Sem uma classe base, cada módulo repete esses campos com nomes, tipos e defaults diferentes. Como ainda não existe nenhum módulo de negócio, este é o momento de fixar o padrão antes da primeira entidade.

## What Changes

- Nova classe base abstrata `Entity`, herdada por todas as classes que representam uma tabela no banco, com os campos:
  - `Id` (`long`): chave primária, gerada pelo banco como identity.
  - `Identifier` (`Guid`): identificador UUID versão 7, gerado na criação da entidade, único na tabela.
  - `CreateIn` (`DateTime`): data e hora da inclusão, default "agora".
  - `UpdateIn` (`DateTime`): data e hora da última alteração, default "agora" e atualizada automaticamente a cada alteração da entidade.
  - `IsActive` (`bool`): indica se o registro está ativo, default `true`.
- O `ModuleDbContext` passa a configurar, por convenção, toda entidade que herda de `Entity` (identity, índice único, defaults no banco) e a preencher `CreateIn`/`UpdateIn` ao salvar. O módulo não precisa configurar esses campos.
- Colunas geradas: `id`, `identifier`, `create_in`, `update_in`, `is_active`, seguindo as convenções de banco existentes (snake_case, `timestamp without time zone`).

Nome do campo de ativo: o pedido original trazia `IsActve`; foi corrigido para `IsActive` (coluna `is_active`). `CreateIn` e `UpdateIn` foram mantidos como pedidos.

## Capabilities

### New Capabilities

- `base-entity`: campos comuns a toda entidade persistida (chave, identificador público, datas de criação/alteração e indicador de ativo), seus tipos, defaults e o preenchimento automático ao salvar.

### Modified Capabilities

Nenhuma. As regras de `database-conventions` (snake_case, datas sem timezone, schema por módulo) continuam as mesmas e se aplicam às colunas da classe base.

## Impact

- **Módulos afetados:** nenhum módulo de negócio (ainda não existem). Mudam os projetos compartilhados `Clavus.Shared.Abstractions` (nova classe `Entity`) e `Clavus.Shared.Infrastructure` (convenção e preenchimento de datas no `ModuleDbContext`). Os módulos futuros herdam o comportamento ao derivar suas entidades de `Entity`.
- **Contrato da API:** nenhuma mudança. Nenhum endpoint ou DTO é criado ou alterado. **Não é breaking.**
- **Impacto no front (`..\UI`):** nenhum imediato.
- **Banco:** nenhuma tabela de produção é criada nesta change. As tabelas dos módulos futuros terão as cinco colunas acima. O default de `identifier` no banco usa `uuidv7()`, disponível a partir do PostgreSQL 18: a versão mínima suportada sobe de 16 para **18** (o `docker-compose.yml` já usa a 18; quem usa PostgreSQL instalado na máquina precisa atualizar).
- **Dependências novas:** nenhuma.

### Fora do escopo

- Exclusão lógica: filtro global por `IsActive`, troca de `DELETE` por desativação ou endpoints de ativar/desativar.
- Auditoria de usuário (quem criou/alterou), histórico de alterações e controle de concorrência otimista.
- Repositório genérico, eventos de domínio ou qualquer outra infraestrutura de domínio além da classe base.
- Regras sobre quais campos os DTOs expõem (por exemplo, expor `Identifier` em vez de `Id`). Fica para a primeira change com endpoint de negócio.
