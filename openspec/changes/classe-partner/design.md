# Design

## Context

- Ainda não há módulos de negócio: `src/Modules/` está vazio e o `ModuleCatalog` também. O README já descreve o passo a passo para criar um módulo (projeto, `ModuleDbContext`, `IModule`, migration com `--startup-project src/Clavus.Api`).
- O `ModuleDbContext` já aplica schema, snake_case, `timestamp without time zone`, a convenção de `Entity` (identity, `identifier` único, defaults) e o preenchimento de `create_in`/`update_in`. Ver specs `database-conventions` e `base-entity`.
- A API não aplica migrations ao subir. Elas são aplicadas com `dotnet ef database update`.
- O host não converte exceções de domínio em 400. Hoje qualquer exceção vira 500 em Problem Details.
- Testes de integração usam `PostgresFixture` (banco temporário), compartilhado com todo projeto em `tests/` pelo `tests/Directory.Build.props`.

## Goals / Non-Goals

**Goals:**
- Um objeto de domínio inválido não pode ser criado: as validações ficam no próprio domínio (construtores e métodos), não nos endpoints.
- A estrutura do primeiro módulo serve de modelo para os próximos.

**Non-Goals:**
- Repositórios, serviços de aplicação ou CQRS. O DbContext do módulo é usado diretamente até existir a primeira change com endpoints.
- Controle de concorrência ou eventos de domínio.

## Decisions

### 1. Nomes em inglês

| Pedido | Classe / propriedade | Coluna |
|---|---|---|
| Partner | `Partner` | tabela `partner` |
| razão social / nome fantasia | `Name` / `TradeName` | `name` / `trade_name` |
| cnpj-cpf | `Document` (objeto de valor) | `document` |
| inscrição municipal / estadual | `MunicipalRegistration` / `StateRegistration` | `municipal_registration` / `state_registration` |
| address | `Address` (objeto de valor) | `address_*` |
| cep, tipo logradouro, endereço, número | `ZipCode`, `StreetType`, `Street`, `Number` | `address_zip_code`, `address_street_type`, `address_street`, `address_number` |
| bairro, cidade, estado, sigla estado | `Neighborhood`, `City`, `State`, `StateCode` | `address_neighborhood`, `address_city`, `address_state`, `address_state_code` |
| isClient / isSupplier | `IsClient` / `IsSupplier` | `is_client` / `is_supplier` |
| Contato (nome, função) | `Contact` (`Name`, `Role`) | tabela `contact` (`name`, `role`) |
| Email (email, tipo) | `Email` (`Address`, `Type`) | tabela `email` (`address`, `type`) |
| Telefone (ddd, número) | `Phone` (`AreaCode`, `Number`) | tabela `phone` (`area_code`, `number`) |

### 2. Estrutura do módulo

```
src/Modules/Clavus.Modules.Partners/
  Domain/          Partner, Contact, Email, Phone, Document, DocumentType, Address, Text (normalização interna)
  Persistence/     PartnersDbContext, *Configuration (IEntityTypeConfiguration)
  Migrations/
  PartnersModule.cs (IModule: registra o DbContext; MapEndpoints vazio por enquanto)
tests/Clavus.Modules.Partners.Tests/
```

- O projeto referencia `Clavus.Shared.Abstractions` e `Clavus.Shared.Infrastructure`, e o `Clavus.Api` referencia o módulo. Não há dependência com outros módulos.
- Schema `partners`, constante `PartnersDbContext.SchemaName`.
- *Alternativa descartada:* projetos separados por camada (Domain/Infrastructure). Para um módulo deste tamanho, pastas bastam. Se o módulo crescer, é possível separar depois sem mudar o banco.

### 3. Validação no domínio com `DomainException`

- Nova `DomainException : Exception` em `Clavus.Shared.Abstractions.Domain`, com mensagem em pt-BR. Fica no Shared porque todos os módulos vão usar, e porque a futura conversão em 400 vai ficar no host.
- Construtores públicos validam e normalizam. Propriedades têm `private set`, e as alterações passam por métodos que revalidam (`Partner.Rename`, `ChangeDocument`, `ChangeAddress`, `ChangeRegistrations`, `AddContact`/`RemoveContact`, `AddEmail`/`RemoveEmail`, `AddPhone`/`RemovePhone`, e o mesmo para e-mails e telefones do `Contact`). `IsClient` e `IsSupplier` têm `set` público, porque não há regra entre eles.
- Para o EF, cada classe tem um construtor privado sem parâmetros, e as coleções ficam em campos `List<T>` expostos como `IReadOnlyCollection<T>`, com acesso por campo.
- Um helper interno (`Text`) concentra trim, vazio → `null`, obrigatório, tamanho máximo e "só dígitos", para que as mensagens sejam iguais em todo o módulo.
- *Alternativa descartada:* resultado (`Result<T>`) em vez de exceção. Exigiria infraestrutura nova no Shared sem ganho enquanto não há endpoints.

### 4. `Document`: objeto de valor com conversor

- `Document` é uma classe imutável com `Value` (string sem formatação) e `Type` (`DocumentType.Individual` para CPF, `DocumentType.Company` para CNPJ). `Type` é calculado a partir do tamanho e não é gravado.
- Normalização: remover `.`, `-`, `/` e espaços e converter para maiúsculas.
- CPF (11): só dígitos, rejeitar dígitos todos iguais, DVs com pesos 10..2 e 11..2.
- CNPJ (14): `[0-9A-Z]{12}[0-9]{2}`, rejeitar caracteres todos iguais. DVs com valor de cada caractere = ASCII − 48, pesos 5,4,3,2,9,8,7,6,5,4,3,2 e 6,5,4,3,2,9,8,7,6,5,4,3,2, resto da divisão por 11 (< 2 → 0, senão 11 − resto). Essa regra também vale para CNPJs só numéricos.
- Persistência: conversor de valor `Document` ↔ `string`, coluna `document varchar(14) not null`, índice único `ix_partner_document`.
- *Alternativa descartada:* `ComplexProperty` para um único valor. O conversor é mais simples, e um índice único sobre propriedade de tipo complexo é mais limitado no EF.

### 5. `Address`: tipo complexo (`ComplexProperty`)

- `Address` é um objeto de valor imutável, cujo construtor valida o CEP (8 dígitos após remover a formatação), os tamanhos e a UF (lista fixa das 27 siglas, convertida para maiúsculas). Para trocar o endereço, cria-se um novo (`Partner.ChangeAddress`).
- Mapeado como `ComplexProperty` obrigatório do `Partner`: as colunas ficam na tabela `partner` com prefixo `address_`. `address_zip_code varchar(8) not null`, os demais campos `varchar(n) null`.
- *Alternativa descartada:* `OwnsOne`. Tipos owned têm identidade e semântica de entidade, enquanto o tipo complexo do EF 10 tem semântica de valor, que é o que o pedido descreve.

### 6. E-mail e telefone com dois donos possíveis

Tabelas únicas `email` e `phone`, cada uma com duas FKs anuláveis, `partner_id` e `contact_id`, e o check constraint `ck_email_owner` (e `ck_phone_owner`) com `num_nonnulls(partner_id, contact_id) = 1`. As FKs são propriedades de sombra, para não aparecerem no domínio. `on delete cascade` nas quatro FKs.

- **Remoção da lista:** em relação opcional, o EF anula a FK do item removido em vez de excluí-lo, o que violaria o check. O `PartnersDbContext` sobrescreve `SaveChanges`/`SaveChangesAsync` e, antes de chamar a base, marca como `Deleted` todo `Email`/`Phone` rastreado cujas duas FKs ficaram nulas.
- A remoção de contato funciona sem tratamento especial. `contact.partner_id` é obrigatório, então o EF exclui o órfão, e o cascade apaga os e-mails e telefones do contato, tanto no change tracker quanto no banco.
- *Alternativa descartada:* coleções owned (`OwnsMany`) para cada dono. Gerariam quatro tabelas (`partner_email`, `contact_email` etc.), enquanto o pedido define uma tabela por classe.
- *Alternativa descartada:* tabelas de ligação. Seriam mais tabelas e joins para relações que são sempre 1:N.

### 7. Banco: schema `partners`

| Tabela | Colunas (além das de `Entity`) | Restrições |
|---|---|---|
| `partner` | `name varchar(100)`, `trade_name varchar(100)`, `document varchar(14)`, `municipal_registration varchar(15) null`, `state_registration varchar(15) null`, `address_*`, `is_client boolean default false`, `is_supplier boolean default false` | `ix_partner_document` único |
| `contact` | `partner_id bigint`, `name varchar(100)`, `role varchar(30) null` | FK → `partner` cascade |
| `email` | `partner_id bigint null`, `contact_id bigint null`, `address varchar(75)`, `type varchar(15)` | FKs cascade, `ck_email_owner` |
| `phone` | `partner_id bigint null`, `contact_id bigint null`, `area_code varchar(2)`, `number varchar(9)` | FKs cascade, `ck_phone_owner` |

- `is_client`/`is_supplier` recebem `HasDefaultValue(false)`, para que inclusões por SQL também fiquem com `false`.
- Datas: só as de `Entity` (`timestamp without time zone`).
- Migration inicial do módulo: `<yyyyMMdd>_Inicial`, com a data do dia da implementação, gerada com `--startup-project src/Clavus.Api`, como no README.

## Risks / Trade-offs

- [O `EFCore.NamingConventions` pode não renomear as colunas de `ComplexProperty` para snake_case] → a tarefa da migration confere os nomes. Se sair `Address_ZipCode`, a configuração define as colunas com `HasColumnName` em minúsculas (`address_zip_code`), o que o CLAUDE.md permite por não usar maiúsculas.
- [Validação de e-mail por `MailAddress.TryCreate` aceita alguns formatos incomuns] → é suficiente para detectar erros de digitação. Validar a existência do e-mail fica fora do escopo.
- [Exceção de domínio vira 500 enquanto não houver mapeamento] → não há endpoints nesta change. O mapeamento para 400 entra na change de endpoints.
- [Rotina de órfãos no `SaveChanges` do módulo] → fica restrita a `Email`/`Phone`, coberta pelos cenários de remoção. O check constraint no banco garante que um erro aqui nunca grave dado inconsistente.

## Migration Plan

1. Fazer o deploy do código.
2. Rodar `dotnet ef database update --project src/Modules/Clavus.Modules.Partners --startup-project src/Clavus.Api --context PartnersDbContext` em cada ambiente.

A migration só cria o schema `partners` e as quatro tabelas, sem tocar em dados existentes. Rollback: `dotnet ef database update 0 --context PartnersDbContext`, que remove as tabelas, e depois reverter o commit.
