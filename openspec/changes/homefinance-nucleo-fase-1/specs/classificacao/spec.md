## Purpose

Atribui categoria aos lançamentos por meio de regras do próprio tenant e transforma cada
correção manual do usuário em regra permanente, para que o mesmo lançamento não precise
ser classificado duas vezes.

## ADDED Requirements

### Requirement: Classificação automática por regra

O sistema SHALL aplicar as regras de classificação do tenant a cada lançamento criado, e
SHALL atribuir a categoria da primeira regra que casar com a descrição.

#### Scenario: Regra existente casa com a descrição
- **WHEN** um lançamento com descrição "PAG*IFOOD" é criado e existe regra que associa
  "IFOOD" à categoria Alimentação
- **THEN** o lançamento é gravado com a categoria Alimentação

#### Scenario: Nenhuma regra casa
- **WHEN** um lançamento é criado e nenhuma regra do tenant casa com sua descrição
- **THEN** o lançamento fica pendente de classificação

#### Scenario: Mais de uma regra casa
- **WHEN** duas regras casam com a mesma descrição
- **THEN** o sistema aplica a regra mais específica, e em caso de empate a mais recente

### Requirement: Lançamentos pendentes de classificação

O sistema SHALL apresentar ao usuário os lançamentos pendentes de classificação e SHALL
permitir classificá-los manualmente.

#### Scenario: Listagem de pendentes
- **WHEN** o usuário abre o extrato do mês
- **THEN** os lançamentos sem categoria aparecem destacados como pendentes, com ação de
  classificar

#### Scenario: Classificação manual
- **WHEN** o usuário atribui a categoria Transporte a um lançamento pendente
- **THEN** o lançamento passa a compor o consumo do orçamento de Transporte no mês de
  competência dele

### Requirement: Correção manual vira regra

Ao classificar ou corrigir manualmente a categoria de um lançamento, o sistema SHALL
oferecer a criação de uma regra que aplique a mesma categoria a lançamentos futuros de
descrição equivalente.

#### Scenario: Regra criada a partir da correção
- **WHEN** o usuário classifica "PAG*XYZ" como Lazer e aceita criar a regra
- **THEN** lançamentos futuros com descrição equivalente são classificados como Lazer
  automaticamente

#### Scenario: Usuário recusa criar a regra
- **WHEN** o usuário classifica um lançamento e recusa a criação da regra
- **THEN** apenas aquele lançamento é classificado, e lançamentos futuros equivalentes
  continuam ficando pendentes

#### Scenario: Correção de categoria já atribuída
- **WHEN** o usuário troca a categoria de um lançamento que havia sido classificado
  automaticamente e aceita criar a regra
- **THEN** a regra anterior deixa de ser aplicada para aquela descrição e a nova passa a
  valer para lançamentos futuros

### Requirement: Classificação manual prevalece

O sistema SHALL NOT sobrescrever, por aplicação de regra, a categoria que o usuário
atribuiu manualmente a um lançamento.

#### Scenario: Nova regra não altera o passado manual
- **WHEN** o usuário cria uma regra que casaria com um lançamento já classificado
  manualmente
- **THEN** a categoria daquele lançamento permanece como o usuário definiu

### Requirement: Gestão das regras

O usuário SHALL poder listar, alterar e remover as regras de classificação do próprio
tenant.

#### Scenario: Remoção de regra
- **WHEN** o usuário remove uma regra
- **THEN** lançamentos futuros deixam de ser classificados por ela, e os lançamentos já
  classificados mantêm suas categorias

#### Scenario: Regras são por tenant
- **WHEN** Bruno visualiza o tenant compartilhado da Ana
- **THEN** ele vê as regras daquele tenant em modo de leitura, e elas não afetam o tenant
  dele
