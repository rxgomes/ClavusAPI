# Spec Delta

## ADDED Requirements

### Requirement: Violação de regra de negócio
Uma violação de regra de negócio ou de validação de domínio lançada por um endpoint SHALL resultar em `400` em Problem Details, com `title` `Requisição inválida.` e a mensagem da regra, em pt-BR, no campo `detail`, em qualquer ambiente. A resposta MUST NOT conter tipo de exceção nem stack trace.

#### Scenario: Regra violada em produção
- **WHEN** a API está em `Production` e um endpoint rejeita a entrada com a regra `CPF inválido.`
- **THEN** a resposta MUST ter status `400`, `title` `Requisição inválida.`, `detail` `CPF inválido.` e MUST NOT conter stack trace

#### Scenario: Erro inesperado continua 500
- **WHEN** a API está em `Production` e um endpoint lança `InvalidOperationException("detalhe interno")`
- **THEN** a resposta MUST continuar com status `500` e MUST NOT conter `detalhe interno`

### Requirement: Conflito com título em pt-BR
Uma resposta `409` gerada pela API SHALL estar em Problem Details com `title` `Conflito.`.

#### Scenario: Recurso já existente
- **WHEN** um endpoint responde `409` por conflito com dado já gravado
- **THEN** a resposta MUST ter `Content-Type: application/problem+json`, `status` `409` e `title` `Conflito.`
