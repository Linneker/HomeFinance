## Purpose

Apresenta o mês fechado sob duas óticas — competência como visão primária e caixa como
secundária — com extrato detalhado que indica, lançamento a lançamento, a qual previsão
orçamentária cada gasto pertence ou se está fora do planejamento.

## ADDED Requirements

### Requirement: Extrato detalhado do mês

O sistema SHALL apresentar, para um mês e tenant, a lista de lançamentos com data,
descrição, valor, categoria e a indicação da previsão orçamentária correspondente.

#### Scenario: Lançamento dentro da previsão
- **WHEN** o extrato de agosto exibe um lançamento de Alimentação e há orçamento definido
  para Alimentação em agosto
- **THEN** o lançamento é exibido vinculado a essa previsão

#### Scenario: Lançamento fora do planejamento
- **WHEN** o extrato exibe um lançamento cuja categoria não tem valor planejado no mês
- **THEN** o lançamento é marcado como fora do planejamento

#### Scenario: Lançamento pendente de classificação
- **WHEN** o extrato exibe um lançamento sem categoria
- **THEN** ele é destacado como pendente, com ação de classificar disponível

#### Scenario: Mês sem lançamentos
- **WHEN** o usuário abre um mês sem nenhum lançamento
- **THEN** o sistema exibe o extrato vazio informando a ausência de registros no período

### Requirement: Alternância entre competência e caixa

O sistema SHALL apresentar os relatórios em regime de competência por padrão e SHALL
permitir alternar para regime de caixa, deixando explícito qual visão está ativa.

#### Scenario: Visão padrão
- **WHEN** o usuário abre o relatório de um mês
- **THEN** o sistema apresenta a visão de competência e a identifica na tela

#### Scenario: Compra parcelada em competência
- **WHEN** existe compra de R$ 300,00 em 3 parcelas feita em julho e o usuário abre julho
  em competência
- **THEN** o relatório apresenta R$ 300,00 em julho

#### Scenario: Mesma compra em caixa
- **WHEN** o usuário alterna para caixa e abre agosto
- **THEN** o relatório apresenta R$ 100,00 referentes à parcela com vencimento em agosto

### Requirement: Resumo por categoria

O sistema SHALL apresentar, por mês e tenant, o total realizado por categoria comparado ao
valor planejado, com a diferença entre eles.

#### Scenario: Categoria dentro do orçamento
- **WHEN** Alimentação tem R$ 1.200,00 planejados e R$ 980,00 de competência realizada em
  agosto
- **THEN** o resumo apresenta a categoria com saldo disponível de R$ 220,00

#### Scenario: Categoria estourada
- **WHEN** Transporte tem R$ 400,00 planejados e R$ 520,00 de competência realizada
- **THEN** o resumo apresenta a categoria como excedida em R$ 120,00

#### Scenario: Gastos sem planejamento
- **WHEN** existem lançamentos em categorias sem valor planejado no mês
- **THEN** o resumo apresenta esses totais agrupados como fora do planejamento

### Requirement: Relatório restrito ao tenant

O relatório SHALL considerar exclusivamente os lançamentos do tenant selecionado, apurados
a partir das participações registradas, e SHALL NOT somar valores de tenants distintos.

#### Scenario: Relatório de tenant compartilhado
- **WHEN** Bruno seleciona o tenant da Ana e abre o relatório de agosto
- **THEN** o relatório apresenta apenas os valores do tenant da Ana, sem qualquer valor do
  tenant do Bruno
