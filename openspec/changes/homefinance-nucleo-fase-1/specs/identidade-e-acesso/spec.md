## Purpose

Estabelece quem é a pessoa que usa o produto: criação de conta, autenticação, encerramento
de sessão e o registro das bases legais e consentimentos exigidos pela LGPD antes de
qualquer dado financeiro ser tratado.

## ADDED Requirements

### Requirement: Criação de conta

O sistema SHALL permitir que uma pessoa crie uma conta informando e-mail e senha, e SHALL
recusar a criação quando o e-mail já estiver em uso.

#### Scenario: Conta criada com sucesso
- **WHEN** uma pessoa informa um e-mail ainda não cadastrado e uma senha válida
- **THEN** a conta é criada, um tenant individual é provisionado para ela e a pessoa é
  autenticada

#### Scenario: E-mail já cadastrado
- **WHEN** uma pessoa tenta criar conta com e-mail já existente
- **THEN** o sistema recusa a criação e informa que o e-mail já está em uso, sem revelar
  qualquer outro dado da conta existente

#### Scenario: Senha fora da política
- **WHEN** a senha informada não atende à política mínima de complexidade
- **THEN** o sistema recusa a criação e descreve os critérios não atendidos

### Requirement: Autenticação

O sistema SHALL autenticar a pessoa por e-mail e senha e SHALL emitir um token de sessão
com prazo de expiração.

#### Scenario: Credenciais válidas
- **WHEN** a pessoa informa e-mail e senha corretos
- **THEN** o sistema emite um token de sessão válido e registra o acesso

#### Scenario: Credenciais inválidas
- **WHEN** e-mail ou senha estão incorretos
- **THEN** o sistema recusa a autenticação com mensagem genérica, sem indicar qual dos
  dois está errado

#### Scenario: Sessão expirada
- **WHEN** uma requisição usa token de sessão expirado
- **THEN** o sistema recusa a requisição e sinaliza que é necessário autenticar de novo

### Requirement: Registro de consentimento

O sistema SHALL registrar, antes de tratar qualquer dado financeiro, o aceite dos termos
de uso e da política de privacidade, guardando data, hora e versão do documento aceito.

#### Scenario: Aceite registrado na criação da conta
- **WHEN** a pessoa cria a conta e aceita os termos
- **THEN** o sistema persiste o aceite com data, hora e versão do documento

#### Scenario: Termos atualizados
- **WHEN** a pessoa autentica e existe versão dos termos mais recente que a aceita por ela
- **THEN** o sistema exige novo aceite antes de liberar operações sobre dados financeiros

#### Scenario: Recusa dos termos
- **WHEN** a pessoa recusa os termos
- **THEN** o sistema não cria a conta e não persiste dado financeiro algum

### Requirement: Exclusão de conta

O sistema SHALL permitir que a pessoa solicite a exclusão da própria conta, e a exclusão
SHALL remover os dados do tenant dela, incluindo os objetos armazenados fora do banco.

#### Scenario: Exclusão solicitada
- **WHEN** a pessoa confirma a exclusão da conta
- **THEN** o sistema remove os dados do tenant dela e os objetos associados no
  armazenamento de arquivos, e registra a execução da exclusão

#### Scenario: Tenant compartilhado com terceiros
- **WHEN** a pessoa exclui a conta e havia compartilhado o tenant dela com outras pessoas
- **THEN** os compartilhamentos são encerrados e os destinatários deixam de ver aqueles
  dados imediatamente
