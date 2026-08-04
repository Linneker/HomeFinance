## Purpose

Define o espaço de dados de cada pessoa — o tenant — o isolamento entre tenants, e o
compartilhamento de visualização de um tenant com outra pessoa, sempre assimétrico,
somente leitura e revogável.

## ADDED Requirements

### Requirement: Tenant individual

O sistema SHALL provisionar exatamente um tenant por conta de usuário no momento da
criação da conta, e todo dado financeiro SHALL pertencer a exatamente um tenant.

#### Scenario: Tenant provisionado com a conta
- **WHEN** uma conta é criada
- **THEN** um tenant individual é criado e vinculado a ela como dono

#### Scenario: Dado sempre vinculado a um tenant
- **WHEN** qualquer registro financeiro é criado
- **THEN** ele é gravado com o identificador do tenant ativo da requisição

### Requirement: Isolamento entre tenants

O sistema SHALL impedir, no nível do banco de dados, que dados de um tenant sejam lidos
ou alterados no contexto de outro tenant, independentemente da consulta emitida pela
aplicação.

#### Scenario: Consulta sem filtro de tenant
- **WHEN** a aplicação emite uma consulta sem restringir explicitamente o tenant
- **THEN** o banco retorna apenas as linhas do tenant ativo da requisição

#### Scenario: Acesso a identificador de outro tenant
- **WHEN** uma requisição autenticada como Ana tenta ler um registro cujo identificador
  pertence ao tenant do Bruno, e Bruno não compartilhou o tenant dele com Ana
- **THEN** o sistema responde como se o registro não existisse

#### Scenario: Escrita cruzada entre tenants
- **WHEN** uma requisição autenticada como Ana tenta alterar um registro do tenant do
  Bruno
- **THEN** a alteração é recusada e nenhuma linha é modificada

### Requirement: Compartilhamento de tenant

O dono de um tenant SHALL poder conceder a outra pessoa acesso somente leitura ao seu
tenant. O compartilhamento SHALL ser assimétrico e SHALL exigir consentimento explícito do
dono, apresentado como lista item a item do que ficará visível. A concessão e a
manutenção do acesso SHALL depender das condições de assinatura definidas na capability
`assinatura`.

#### Scenario: Compartilhamento concedido
- **WHEN** Ana confirma o compartilhamento do tenant dela com Bruno após a tela de
  consentimento itemizada, com assinatura ativa e cota disponível no plano
- **THEN** Bruno passa a poder selecionar e visualizar o tenant da Ana, além do próprio

#### Scenario: Assimetria preservada
- **WHEN** Ana compartilha o tenant dela com Bruno
- **THEN** Ana continua sem acesso ao tenant do Bruno, a menos que Bruno compartilhe o
  dele com ela

#### Scenario: Consentimento não confirmado
- **WHEN** Ana inicia o compartilhamento mas não confirma a tela de consentimento
- **THEN** nenhum acesso é concedido

#### Scenario: Categoria sensível exige opt-in
- **WHEN** a tela de consentimento apresenta uma categoria de dado marcada como sensível
- **THEN** essa categoria fica fora do compartilhamento por padrão e só é incluída se o
  dono a marcar explicitamente

### Requirement: Compartilhamento é somente leitura

Uma pessoa que recebeu acesso a um tenant de terceiro SHALL poder apenas visualizar seus
dados, e SHALL ser impedida de criar, alterar ou remover qualquer registro nele.

#### Scenario: Tentativa de lançamento em tenant de terceiro
- **WHEN** Bruno, com acesso de leitura ao tenant da Ana, tenta criar um lançamento nesse
  tenant
- **THEN** o sistema recusa a operação

#### Scenario: Visualização permitida
- **WHEN** Bruno seleciona o tenant da Ana
- **THEN** o sistema exibe lançamentos, orçamento e relatórios daquele tenant em modo de
  leitura

### Requirement: Ausência de agregação entre tenants

O sistema SHALL apresentar relatórios e totais sempre no escopo de um único tenant, e
SHALL NOT somar valores de tenants distintos.

#### Scenario: Troca de visão sem soma
- **WHEN** Bruno alterna entre o tenant dele e o tenant compartilhado da Ana
- **THEN** cada visão exibe apenas os valores do respectivo tenant, sem consolidação entre
  eles

### Requirement: Revogação e repasse do compartilhamento

Apenas o dono do tenant SHALL poder conceder ou revogar o compartilhamento, e a revogação
SHALL ter efeito imediato.

#### Scenario: Revogação imediata
- **WHEN** Ana revoga o compartilhamento com Bruno
- **THEN** Bruno perde o acesso ao tenant da Ana na requisição seguinte

#### Scenario: Repasse não permitido
- **WHEN** Bruno, que tem acesso de leitura ao tenant da Ana, tenta compartilhá-lo com
  Carla
- **THEN** o sistema recusa a operação
