# Tasks

## 1. Clavus.Shared.Abstractions: exceção de domínio

- [x] 1.1 Criar `Domain/DomainException.cs` (`Exception` com construtor recebendo a mensagem em pt-BR), com XML doc. Verificar com `dotnet build` sem warnings.

## 2. Partners: projetos

- [x] 2.1 Criar `src/Modules/Clavus.Modules.Partners` seguindo o README (classlib na solução em `/src/Modules/`, referências a `Clavus.Shared.Abstractions` e `Clavus.Shared.Infrastructure`, sem `Class1.cs` e sem as propriedades que vêm do `Directory.Build.props`). Verificar com `dotnet build` sem warnings.
- [x] 2.2 Criar `tests/Clavus.Modules.Partners.Tests` (na solução em `/tests/`, referenciando o módulo e `Microsoft.EntityFrameworkCore.Design`, no mesmo molde de `Clavus.Shared.Infrastructure.Tests`). Verificar que `dotnet test` roda o projeto, ainda sem testes.

## 3. Partners: objetos de valor

- [x] 3.1 Criar o helper interno `Domain/Text.cs` (trim, vazio → `null`, obrigatório, tamanho máximo, remover formatação/só dígitos), com mensagens em pt-BR. Verificar com `dotnet build` sem warnings.
- [x] 3.2 Criar `Domain/DocumentType.cs` e `Domain/Document.cs`: normalização, CPF com DV, CNPJ alfanumérico com DV (ASCII − 48), rejeição de caracteres repetidos, igualdade por valor. Criar `DocumentTests` com os cenários de "Documento CPF ou CNPJ", "Validação do CPF" e "Validação do CNPJ alfanumérico" (`52998224725`, `11222333000181`, `12ABC34501DE35`, minúsculas, DV errado, repetidos, letra no CPF e no DV, tamanho inválido, vazio). Verificar que `dotnet test` passa.
- [x] 3.3 Criar `Domain/Address.cs`: CEP obrigatório de 8 dígitos sem formatação, tamanhos máximos, campos opcionais vazios → `null`, UF em maiúsculas validada contra as 27 siglas. Criar `AddressTests` com os cenários de "Endereço do parceiro" e "UF do endereço". Verificar que `dotnet test` passa.

## 4. Partners: entidades

- [x] 4.1 Criar `Domain/Email.cs` e `Domain/Phone.cs` (herdam `Entity`): validação do e-mail (`MailAddress.TryCreate`, até 75) e do tipo (obrigatório, até 15); DDD de 2 dígitos entre 11 e 99, número com 8 ou 9 dígitos, ambos sem formatação. Criar `EmailTests` e `PhoneTests` com os cenários de validação das specs. Verificar que `dotnet test` passa.
- [x] 4.2 Criar `Domain/Contact.cs` (herda `Entity`): nome obrigatório (100), função opcional (30), listas de e-mails e telefones com `Add*`/`Remove*`. Criar `ContactTests` (contato sem nome, função longa demais, adicionar e remover itens). Verificar que `dotnet test` passa.
- [x] 4.3 Criar `Domain/Partner.cs` (herda `Entity`): `Name`/`TradeName` obrigatórios (100), `Document`, inscrições opcionais (15), `Address`, `IsClient`/`IsSupplier` (padrão `false`), listas de contatos, e-mails e telefones, e os métodos de alteração do design. Criar `PartnerTests` com os cenários de "Dados obrigatórios do parceiro", "Inscrições municipal e estadual" e os indicadores padrão. Verificar que `dotnet test` passa.

## 5. Partners: persistência e migration

- [x] 5.1 Criar `Persistence/PartnersDbContext.cs` (`ModuleDbContext`, schema `partners`) e as configurações: conversor e índice único de `Document`; `ComplexProperty` de `Address`; tamanhos `varchar(n)`; defaults `false` de `is_client`/`is_supplier`; `contact` → `partner` cascade; FKs de sombra `partner_id`/`contact_id` em `email`/`phone` com cascade e os check constraints `ck_email_owner`/`ck_phone_owner` (`num_nonnulls(partner_id, contact_id) = 1`); coleções com acesso por campo. Sobrescrever `SaveChanges`/`SaveChangesAsync` para excluir `Email`/`Phone` órfãos. Verificar com `dotnet build` sem warnings.
- [x] 5.2 Criar `PartnersModule.cs` (`IModule`, `Name = "Partners"`, registra o DbContext com `AddModuleDbContext`, `MapEndpoints` sem endpoints), referenciar o módulo em `Clavus.Api` e incluí-lo no `ModuleCatalog`. Verificar com `dotnet build` sem warnings e com os testes de `Clavus.Api.Tests` ainda passando.
- [x] 5.3 Gerar a migration inicial com o prefixo da data do dia (`dotnet ef migrations add <yyyyMMdd>_Inicial --project src/Modules/Clavus.Modules.Partners --startup-project src/Clavus.Api --context PartnersDbContext`). Conferir no arquivo gerado: schema `partners`; tabelas `partner`, `contact`, `email`, `phone`; colunas `address_*` em snake_case (se não estiverem, aplicar o plano B do design e gerar de novo); `varchar` com os tamanhos do design; índice único `ix_partner_document`; check constraints; FKs cascade; nenhum `timestamp with time zone`.
- [x] 5.4 Criar `PartnersPersistenceTests` (banco temporário via `PostgresFixture`, aplicando a migration) cobrindo: gravação e leitura completa (endereço, documento, contatos, e-mails e telefones de cada dono); documento sem formatação; documento duplicado rejeitado (`23505`); indicadores padrão `false` e `true` persistido; e-mail/telefone sem dono ou com dois donos rejeitado por SQL (`23514`); remover e-mail e telefone do parceiro e do contato; remover contato; excluir parceiro em cascata. Verificar que `dotnet test` passa.

## 6. Documentação e verificação final

- [x] 6.1 Atualizar o `README.md`: módulo `Partners` na seção "Estrutura", comando de `database update` do módulo e uma nota sobre `DomainException` (validações no domínio, mensagens em pt-BR, ainda sem mapeamento para 400). Verificar que os comandos do README rodam como escritos.
- [x] 6.2 Verificação integrada: `dotnet build` sem warnings, `dotnet test` com todos os testes passando (inclusive os 50 existentes), e `dotnet ef database update` do módulo aplicado no banco de desenvolvimento, conferindo com `select * from partners.partner` sem aspas.

## Workflow follow-up

- Arquivar a change com `/opsx:archive` após revisão.
