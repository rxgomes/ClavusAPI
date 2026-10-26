# Spec Delta

## Purpose

Define o cadastro de parceiros de negócio (clientes e fornecedores): dados do parceiro, documento CPF/CNPJ, endereço, contatos, e-mails e telefones, com as validações e regras de unicidade que valem para qualquer forma de inclusão ou alteração.

## ADDED Requirements

### Requirement: Dados obrigatórios do parceiro
Todo parceiro MUST ter razão social (ou nome) e nome fantasia preenchidos, com até 100 caracteres cada. Valores só com espaços MUST ser rejeitados, e os espaços nas pontas MUST ser removidos antes de gravar.

#### Scenario: Parceiro com razão social e nome fantasia
- **WHEN** um parceiro é criado com razão social `" Ixact Tecnologia Ltda "` e nome fantasia `"Ixact"`
- **THEN** o parceiro MUST ser aceito com a razão social `"Ixact Tecnologia Ltda"`

#### Scenario: Nome fantasia em branco
- **WHEN** um parceiro é criado com nome fantasia vazio ou só com espaços
- **THEN** a criação MUST ser rejeitada com mensagem em pt-BR informando que o nome fantasia é obrigatório

#### Scenario: Razão social longa demais
- **WHEN** um parceiro é criado com razão social de 101 caracteres
- **THEN** a criação MUST ser rejeitada com mensagem em pt-BR informando o tamanho máximo

### Requirement: Documento CPF ou CNPJ
Todo parceiro MUST ter um documento válido. A formatação (`.`, `-`, `/` e espaços) MUST ser removida e as letras MUST ficar em maiúsculas antes da validação. O documento MUST ser gravado sem formatação.

#### Scenario: CPF formatado
- **WHEN** um parceiro é criado com o documento `"529.982.247-25"`
- **THEN** o documento gravado MUST ser `"52998224725"` e o parceiro MUST ser pessoa física

#### Scenario: CNPJ numérico formatado
- **WHEN** um parceiro é criado com o documento `"11.222.333/0001-81"`
- **THEN** o documento gravado MUST ser `"11222333000181"` e o parceiro MUST ser pessoa jurídica

#### Scenario: Documento com tamanho inválido
- **WHEN** um parceiro é criado com um documento que, sem formatação, não tem 11 nem 14 caracteres
- **THEN** a criação MUST ser rejeitada com mensagem em pt-BR informando que o documento é inválido

#### Scenario: Documento vazio
- **WHEN** um parceiro é criado sem documento
- **THEN** a criação MUST ser rejeitada com mensagem em pt-BR informando que o documento é obrigatório

### Requirement: Validação do CPF
Um documento de 11 caracteres MUST ser um CPF: só dígitos, com os dois dígitos verificadores corretos. Sequências de dígitos todos iguais MUST ser rejeitadas.

#### Scenario: CPF com dígito verificador errado
- **WHEN** um parceiro é criado com o documento `"52998224726"`
- **THEN** a criação MUST ser rejeitada com mensagem em pt-BR informando que o CPF é inválido

#### Scenario: CPF com dígitos repetidos
- **WHEN** um parceiro é criado com o documento `"111.111.111-11"`
- **THEN** a criação MUST ser rejeitada

#### Scenario: CPF com letra
- **WHEN** um parceiro é criado com um documento de 11 caracteres que contém letra
- **THEN** a criação MUST ser rejeitada

### Requirement: Validação do CNPJ alfanumérico
Um documento de 14 caracteres MUST ser um CNPJ: 12 primeiros caracteres em `0-9` ou `A-Z` e 2 dígitos verificadores numéricos, calculados pela regra oficial do CNPJ alfanumérico, na qual cada caractere vale seu código ASCII menos 48. CNPJs só numéricos MUST continuar válidos. Sequências de caracteres todos iguais MUST ser rejeitadas.

#### Scenario: CNPJ alfanumérico válido
- **WHEN** um parceiro é criado com o documento `"12.ABC.345/01DE-35"`
- **THEN** o documento gravado MUST ser `"12ABC34501DE35"` e o parceiro MUST ser pessoa jurídica

#### Scenario: CNPJ alfanumérico em minúsculas
- **WHEN** um parceiro é criado com o documento `"12abc34501de35"`
- **THEN** o documento gravado MUST ser `"12ABC34501DE35"`

#### Scenario: CNPJ com dígito verificador errado
- **WHEN** um parceiro é criado com o documento `"12ABC34501DE36"`
- **THEN** a criação MUST ser rejeitada com mensagem em pt-BR informando que o CNPJ é inválido

#### Scenario: CNPJ com letra no dígito verificador
- **WHEN** um parceiro é criado com um documento de 14 caracteres cujas duas últimas posições não são dígitos
- **THEN** a criação MUST ser rejeitada

### Requirement: Documento único
Não MUST existir dois parceiros com o mesmo documento, estejam eles ativos ou inativos.

#### Scenario: Documento já cadastrado
- **WHEN** um parceiro é gravado com um documento que já pertence a outro parceiro, mesmo que formatado de outro jeito
- **THEN** a gravação MUST ser rejeitada pelo banco

### Requirement: Inscrições municipal e estadual
As inscrições municipal e estadual MUST ser opcionais, com até 15 caracteres cada. Os espaços nas pontas MUST ser removidos, e um valor só com espaços MUST ser gravado como ausente.

#### Scenario: Parceiro sem inscrições
- **WHEN** um parceiro é criado sem inscrição municipal nem estadual
- **THEN** o parceiro MUST ser aceito e as duas inscrições MUST ficar vazias (`null`)

#### Scenario: Inscrição longa demais
- **WHEN** um parceiro é criado com inscrição estadual de 16 caracteres
- **THEN** a criação MUST ser rejeitada com mensagem em pt-BR informando o tamanho máximo

### Requirement: Endereço do parceiro
Todo parceiro MUST ter um endereço com CEP de 8 dígitos, gravado sem formatação. Tipo de logradouro (5), logradouro (100), número (10), bairro (100), cidade (100) e estado (35) MUST ser opcionais e respeitar esses tamanhos máximos.

#### Scenario: CEP formatado
- **WHEN** um parceiro é criado com o CEP `"01310-100"`
- **THEN** o CEP gravado MUST ser `"01310100"`

#### Scenario: CEP inválido
- **WHEN** um parceiro é criado com um CEP que, sem formatação, não tem exatamente 8 dígitos
- **THEN** a criação MUST ser rejeitada com mensagem em pt-BR informando que o CEP é inválido

#### Scenario: Endereço só com CEP
- **WHEN** um parceiro é criado com endereço contendo apenas o CEP
- **THEN** o parceiro MUST ser aceito e os demais campos do endereço MUST ficar vazios (`null`)

#### Scenario: Campo de endereço longo demais
- **WHEN** um parceiro é criado com logradouro de 101 caracteres
- **THEN** a criação MUST ser rejeitada com mensagem em pt-BR informando o tamanho máximo

### Requirement: UF do endereço
A UF do endereço MUST ser opcional. Quando informada, MUST ser convertida para maiúsculas e MUST ser a sigla de uma das 27 unidades federativas do Brasil.

#### Scenario: UF em minúsculas
- **WHEN** um parceiro é criado com UF `"sp"`
- **THEN** a UF gravada MUST ser `"SP"`

#### Scenario: UF inexistente
- **WHEN** um parceiro é criado com UF `"XX"`
- **THEN** a criação MUST ser rejeitada com mensagem em pt-BR informando que a UF é inválida

### Requirement: Indicadores de cliente e fornecedor
O parceiro MUST ter os indicadores de cliente e de fornecedor, ambos com valor padrão `false`. Os dois são independentes: o parceiro MAY ser cliente, fornecedor, ambos ou nenhum.

#### Scenario: Parceiro novo sem indicadores
- **WHEN** um parceiro é criado sem informar os indicadores e é gravado
- **THEN** os dois indicadores gravados MUST ser `false`

#### Scenario: Parceiro cliente e fornecedor
- **WHEN** um parceiro é marcado como cliente e como fornecedor e é gravado
- **THEN** os dois indicadores lidos de volta MUST ser `true`

### Requirement: Contatos do parceiro
Um parceiro MUST poder ter zero ou mais contatos. Cada contato MUST ter nome obrigatório com até 100 caracteres e função opcional com até 30 caracteres.

#### Scenario: Parceiro com contatos
- **WHEN** um parceiro é gravado com dois contatos e lido de volta
- **THEN** os dois contatos MUST ser lidos com nome e função

#### Scenario: Contato sem nome
- **WHEN** um contato é adicionado sem nome
- **THEN** a inclusão MUST ser rejeitada com mensagem em pt-BR informando que o nome do contato é obrigatório

### Requirement: E-mails do parceiro e do contato
O parceiro e cada contato MUST poder ter zero ou mais e-mails. Cada e-mail MUST ter endereço obrigatório, em formato válido e com até 75 caracteres, e tipo obrigatório com até 15 caracteres, em texto livre. Cada e-mail MUST pertencer a exatamente um dono: o parceiro ou um contato.

#### Scenario: E-mails do parceiro e do contato
- **WHEN** um parceiro é gravado com um e-mail próprio e um contato com outro e-mail, e é lido de volta
- **THEN** cada e-mail MUST aparecer apenas na lista do seu dono

#### Scenario: E-mail em formato inválido
- **WHEN** um e-mail `"financeiro.ixact.com"` é adicionado
- **THEN** a inclusão MUST ser rejeitada com mensagem em pt-BR informando que o e-mail é inválido

#### Scenario: E-mail sem tipo
- **WHEN** um e-mail é adicionado sem tipo
- **THEN** a inclusão MUST ser rejeitada com mensagem em pt-BR informando que o tipo do e-mail é obrigatório

#### Scenario: Remover e-mail
- **WHEN** um e-mail é removido da lista do parceiro ou do contato e o parceiro é gravado
- **THEN** o e-mail MUST ser excluído do banco, e os demais e-mails MUST permanecer

#### Scenario: E-mail sem dono no banco
- **WHEN** um e-mail é incluído por SQL sem parceiro nem contato, ou com os dois
- **THEN** o banco MUST rejeitar a inclusão

### Requirement: Telefones do parceiro e do contato
O parceiro e cada contato MUST poder ter zero ou mais telefones. Cada telefone MUST ter DDD de 2 dígitos entre `11` e `99` e número de 8 ou 9 dígitos, ambos gravados sem formatação. Cada telefone MUST pertencer a exatamente um dono: o parceiro ou um contato.

#### Scenario: Telefone formatado
- **WHEN** um telefone é adicionado com DDD `"(11)"` e número `"98765-4321"`
- **THEN** o DDD gravado MUST ser `"11"` e o número gravado MUST ser `"987654321"`

#### Scenario: Telefone fixo
- **WHEN** um telefone é adicionado com número `"3333-4444"`
- **THEN** o número gravado MUST ser `"33334444"`

#### Scenario: DDD inválido
- **WHEN** um telefone é adicionado com DDD `"01"` ou com um DDD que não tem 2 dígitos
- **THEN** a inclusão MUST ser rejeitada com mensagem em pt-BR informando que o DDD é inválido

#### Scenario: Número com tamanho inválido
- **WHEN** um telefone é adicionado com número de 7 ou 10 dígitos
- **THEN** a inclusão MUST ser rejeitada com mensagem em pt-BR informando que o número é inválido

#### Scenario: Remover telefone
- **WHEN** um telefone é removido da lista do parceiro ou do contato e o parceiro é gravado
- **THEN** o telefone MUST ser excluído do banco, e os demais telefones MUST permanecer

#### Scenario: Telefone sem dono no banco
- **WHEN** um telefone é incluído por SQL sem parceiro nem contato, ou com os dois
- **THEN** o banco MUST rejeitar a inclusão

### Requirement: Exclusão em cascata
A exclusão física de um parceiro MUST excluir os contatos, e-mails e telefones dele. A exclusão de um contato MUST excluir os e-mails e telefones do contato.

#### Scenario: Excluir parceiro
- **WHEN** um parceiro com contatos, e-mails e telefones é excluído
- **THEN** nenhum contato, e-mail ou telefone desse parceiro ou dos contatos dele MUST permanecer no banco

#### Scenario: Remover contato do parceiro
- **WHEN** um contato com e-mails e telefones é removido do parceiro e o parceiro é gravado
- **THEN** o contato e os e-mails e telefones dele MUST ser excluídos, e os do parceiro MUST permanecer
