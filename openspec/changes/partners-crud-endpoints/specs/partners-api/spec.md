# Spec Delta

## Purpose

Define o contrato HTTP do cadastro de parceiros (clientes e fornecedores): inclusão, pesquisa, consulta detalhada, alteração e inativação, com as regras dos filtros de pesquisa e os códigos de resposta que o front consome.

## ADDED Requirements

### Requirement: Inclusão de parceiro
`POST /partners` SHALL incluir um parceiro com dados, endereço, inscrições, indicadores, e-mails, telefones e contatos (com seus e-mails e telefones). Em caso de sucesso, a resposta MUST ter status `201` e o corpo MUST ser o parceiro completo, como em `GET /partners/details`, incluindo `id` e `identifier` gerados.

#### Scenario: Inclusão completa
- **WHEN** um cliente envia `POST /partners` com razão social, nome fantasia, CPF `"529.982.247-25"`, CEP `"01310-100"`, `isClient: true`, um e-mail, um telefone e um contato com e-mail
- **THEN** a resposta MUST ter status `201`, `document` MUST ser `"52998224725"`, `isActive` MUST ser `true` e o corpo MUST conter o e-mail, o telefone e o contato informados

#### Scenario: Normalização aplicada
- **WHEN** um parceiro é incluído com UF `"sp"` e telefone `"(11)"` / `"98765-4321"`
- **THEN** o corpo da resposta MUST trazer UF `"SP"`, DDD `"11"` e número `"987654321"`

### Requirement: Parceiro deve ser cliente ou fornecedor
Na inclusão e na alteração pela API, o parceiro MUST ter `isClient` ou `isSupplier` igual a `true`; os dois MAY ser `true`. Com os dois `false`, a resposta MUST ser `400` com `errors` contendo as chaves `IsClient` e `IsSupplier` e mensagem em pt-BR.

#### Scenario: Nenhum indicador
- **WHEN** um cliente envia `POST /partners` com `isClient: false` e `isSupplier: false`
- **THEN** a resposta MUST ter status `400` e `errors` MUST conter `IsClient` e `IsSupplier`

#### Scenario: Cliente e fornecedor
- **WHEN** um cliente envia `POST /partners` com `isClient: true` e `isSupplier: true`
- **THEN** a resposta MUST ter status `201` e os dois indicadores MUST ser `true` no corpo

### Requirement: Validação de campos da requisição
Campos obrigatórios ausentes e textos acima do tamanho máximo na inclusão e na alteração MUST resultar em `400` com `title` `Um ou mais campos são inválidos.` e `errors` com mensagens em pt-BR por campo, com a chave no nome da propriedade em PascalCase (como nas demais validações da API, ex.: `TradeName`, `Contacts[0].Emails[0].Type`), incluindo campos de endereço, contatos, e-mails e telefones.

#### Scenario: Nome fantasia ausente
- **WHEN** um cliente envia `POST /partners` sem `tradeName`
- **THEN** a resposta MUST ter status `400` e `errors` MUST conter a chave `TradeName` com mensagem em pt-BR

#### Scenario: E-mail de contato sem tipo
- **WHEN** um cliente envia `POST /partners` com um contato cujo e-mail não tem `type`
- **THEN** a resposta MUST ter status `400` e `errors` MUST conter uma chave referente ao `type` desse e-mail

### Requirement: Violação de regra do parceiro
Valores que violam as regras da capability `partners` (CPF/CNPJ, CEP, UF, DDD, número de telefone ou formato de e-mail inválidos) MUST resultar em `400` em Problem Details, com a mensagem da regra em pt-BR no campo `detail`, e nada MUST ser gravado.

#### Scenario: CPF inválido
- **WHEN** um cliente envia `POST /partners` com o documento `"52998224726"`
- **THEN** a resposta MUST ter status `400` e `detail` MUST ser `CPF inválido.`

#### Scenario: UF inválida na alteração
- **WHEN** um cliente envia `PUT /partners/{identifier}` com UF `"XX"`
- **THEN** a resposta MUST ter status `400`, `detail` MUST ser `UF inválida.` e o parceiro MUST permanecer como estava

### Requirement: Documento duplicado
A inclusão ou alteração com um documento que já pertence a outro parceiro, ativo ou inativo, MUST resultar em `409` em Problem Details, com `detail` em pt-BR informando que o documento já está cadastrado.

#### Scenario: Inclusão com documento existente formatado de outro jeito
- **WHEN** existe um parceiro com documento `"11222333000181"` e um cliente envia `POST /partners` com o documento `"11.222.333/0001-81"`
- **THEN** a resposta MUST ter status `409` e nenhum parceiro novo MUST ser gravado

#### Scenario: Alteração para documento de outro parceiro
- **WHEN** um cliente envia `PUT /partners/{identifier}` trocando o documento para o de outro parceiro
- **THEN** a resposta MUST ter status `409`

### Requirement: Filtro de tipo obrigatório nas pesquisas
Todo endpoint de pesquisa de parceiros MUST exigir exatamente um filtro de tipo: `isClient=true` ou `isSupplier=true`. Ausência dos dois, presença dos dois ou valor `false` MUST resultar em `400` com `errors` contendo as chaves `IsClient` e `IsSupplier`. O resultado MUST conter só parceiros do tipo pedido.

#### Scenario: Pesquisa de clientes
- **WHEN** existem um parceiro só cliente, um só fornecedor e um cliente e fornecedor, e um cliente chama `GET /partners?isClient=true`
- **THEN** o resultado MUST conter o parceiro só cliente e o cliente e fornecedor, e MUST NOT conter o parceiro só fornecedor

#### Scenario: Os dois filtros
- **WHEN** um cliente chama `GET /partners?isClient=true&isSupplier=true`
- **THEN** a resposta MUST ter status `400` e `errors` MUST conter `IsClient` e `IsSupplier`

#### Scenario: Nenhum filtro
- **WHEN** um cliente chama `GET /partners` sem `isClient` nem `isSupplier`
- **THEN** a resposta MUST ter status `400`

#### Scenario: Filtro com valor falso
- **WHEN** um cliente chama `GET /partners?isClient=false`
- **THEN** a resposta MUST ter status `400`

### Requirement: Pesquisa por id ou identifier
Todo endpoint de pesquisa de parceiros MUST aceitar `id` ou `identifier`, nunca os dois. Com os dois, a resposta MUST ser `400` com `errors` contendo as chaves `Id` e `Identifier`. O filtro informado MUST ser combinado com o filtro de tipo.

#### Scenario: id e identifier juntos
- **WHEN** um cliente chama `GET /partners?isClient=true&id=1&identifier=0199f5a2-0000-7000-8000-000000000000`
- **THEN** a resposta MUST ter status `400` e `errors` MUST conter `Id` e `Identifier`

#### Scenario: Pesquisa por identifier
- **WHEN** um cliente chama `GET /partners?isSupplier=true&identifier=<identifier de um fornecedor>`
- **THEN** o resultado MUST conter só esse parceiro

#### Scenario: id de parceiro de outro tipo
- **WHEN** um cliente chama `GET /partners?isSupplier=true&id=<id de um parceiro só cliente>`
- **THEN** o resultado MUST estar vazio

### Requirement: Pesquisa paginada de parceiros
`GET /partners` SHALL devolver `items`, `page`, `pageSize` e `totalCount`, com cada item contendo `id`, `identifier`, `name`, `tradeName`, `document`, `documentType`, `isClient`, `isSupplier`, `isActive`, `city` e `stateCode`. A ordenação MUST ser por `tradeName` e depois `id`. `page` MUST ter padrão `1` e mínimo `1`; `pageSize` MUST ter padrão `20` e ficar entre `1` e `100`.

#### Scenario: Segunda página
- **WHEN** existem 25 clientes ativos e um cliente chama `GET /partners?isClient=true&page=2&pageSize=20`
- **THEN** `items` MUST ter 5 parceiros, `page` MUST ser `2` e `totalCount` MUST ser `25`

#### Scenario: Tamanho de página inválido
- **WHEN** um cliente chama `GET /partners?isClient=true&pageSize=101`
- **THEN** a resposta MUST ter status `400` e `errors` MUST conter `PageSize`

### Requirement: Filtro de situação na pesquisa
`GET /partners` MUST aceitar `isActive`, com padrão `true`. Com `isActive=true` o resultado MUST conter só parceiros ativos; com `isActive=false`, só inativos.

#### Scenario: Inativo fora da pesquisa padrão
- **WHEN** um cliente foi inativado e um cliente chama `GET /partners?isClient=true`
- **THEN** o resultado MUST NOT conter o parceiro inativado

#### Scenario: Listar inativos
- **WHEN** um cliente chama `GET /partners?isClient=true&isActive=false`
- **THEN** o resultado MUST conter só clientes inativos

### Requirement: Consulta detalhada de parceiro
`GET /partners/details` SHALL devolver o parceiro completo: dados, endereço, inscrições, indicadores, `isActive`, `createIn`, `updateIn`, e-mails, telefones e contatos com seus e-mails e telefones, cada item com seu `identifier`. Além do filtro de tipo, MUST exigir `id` ou `identifier`. Parceiros inativos MUST ser devolvidos.

#### Scenario: Consulta por id
- **WHEN** um cliente chama `GET /partners/details?isClient=true&id=<id de um cliente>`
- **THEN** a resposta MUST ter status `200` com o parceiro completo

#### Scenario: Sem id nem identifier
- **WHEN** um cliente chama `GET /partners/details?isClient=true`
- **THEN** a resposta MUST ter status `400` e `errors` MUST conter `Id` e `Identifier`

#### Scenario: Parceiro de outro tipo
- **WHEN** um cliente chama `GET /partners/details?isSupplier=true&id=<id de um parceiro só cliente>`
- **THEN** a resposta MUST ter status `404` em Problem Details

#### Scenario: Parceiro inexistente
- **WHEN** um cliente chama `GET /partners/details?isClient=true&identifier=<uuid inexistente>`
- **THEN** a resposta MUST ter status `404` em Problem Details

### Requirement: Alteração de parceiro
`PUT /partners/{identifier}` SHALL substituir dados, endereço, inscrições, indicadores, situação (`isActive`) e as listas de e-mails, telefones e contatos pelo conteúdo enviado, e responder `200` com o parceiro completo. Identifier inexistente MUST resultar em `404`.

#### Scenario: Reativar parceiro
- **WHEN** um cliente envia `PUT /partners/{identifier}` de um parceiro inativo com `isActive: true`
- **THEN** a resposta MUST ter status `200` e o parceiro MUST voltar a aparecer em `GET /partners` com o filtro do seu tipo

#### Scenario: Parceiro inexistente
- **WHEN** um cliente envia `PUT /partners/{identifier}` com um identifier que não existe
- **THEN** a resposta MUST ter status `404`

### Requirement: Listas substituídas na alteração
Na alteração, e-mails e telefones do parceiro e dos contatos MUST ficar iguais aos enviados, e os ausentes MUST ser excluídos. Um contato enviado com `identifier` MUST manter esse `identifier`; sem `identifier`, MUST ser incluído como novo. Contatos ausentes MUST ser excluídos com seus e-mails e telefones. Um `identifier` de contato que não pertence ao parceiro MUST resultar em `400`.

#### Scenario: Remover e-mail e manter contato
- **WHEN** um parceiro tem dois e-mails e um contato, e um cliente envia `PUT` com um só desses e-mails e o contato com o mesmo `identifier` e outro nome
- **THEN** o parceiro MUST ficar com um e-mail, e o contato MUST manter o `identifier` com o nome novo

#### Scenario: Remover contato
- **WHEN** um cliente envia `PUT` sem um contato que o parceiro tinha
- **THEN** o contato e os e-mails e telefones dele MUST ser excluídos

#### Scenario: Contato de outro parceiro
- **WHEN** um cliente envia `PUT` com um contato cujo `identifier` pertence a outro parceiro
- **THEN** a resposta MUST ter status `400` e nada MUST ser alterado

### Requirement: Inativação de parceiro
`DELETE /partners/{identifier}` SHALL inativar o parceiro (`isActive = false`) sem excluir dados, e responder `204`. Inativar um parceiro já inativo MUST responder `204`. Identifier inexistente MUST resultar em `404`.

#### Scenario: Inativar
- **WHEN** um cliente chama `DELETE /partners/{identifier}` de um parceiro ativo
- **THEN** a resposta MUST ter status `204`, e `GET /partners/details` desse parceiro MUST mostrar `isActive: false` com contatos, e-mails e telefones preservados

#### Scenario: Inexistente
- **WHEN** um cliente chama `DELETE /partners/{identifier}` com um identifier que não existe
- **THEN** a resposta MUST ter status `404`

### Requirement: Contrato documentado no OpenAPI
Os endpoints de `/partners`, seus parâmetros, corpos de requisição e respostas (incluindo `400`, `404` e `409`) MUST aparecer no documento OpenAPI agrupados sob a tag `Parceiros`.

#### Scenario: Endpoints no documento
- **WHEN** um cliente obtém `/openapi/v1.json` em `Development`
- **THEN** o documento MUST conter os caminhos `/partners`, `/partners/details` e `/partners/{identifier}` sob a tag `Parceiros`
