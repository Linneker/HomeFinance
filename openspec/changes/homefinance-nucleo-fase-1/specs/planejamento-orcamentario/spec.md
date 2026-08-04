## Purpose

Permite que a pessoa declare sua renda e defina, por categoria e por mês, quanto pretende
gastar — a referência contra a qual os gastos reais são confrontados nos relatórios e nos
alertas.

## ADDED Requirements

### Requirement: Renda declarada

O sistema SHALL registrar a renda mensal informada pelo usuário e SHALL NOT inferi-la a
partir de lançamentos.

#### Scenario: Renda registrada
- **WHEN** o usuário informa uma renda mensal recorrente de R$ 6.000,00
- **THEN** o sistema passa a considerar esse valor como renda prevista de cada mês, com
  competência e caixa no dia informado

#### Scenario: Mais de uma fonte de renda
- **WHEN** o usuário cadastra duas fontes de renda
- **THEN** a renda prevista do mês é a soma das fontes ativas naquele mês

#### Scenario: Alteração de renda
- **WHEN** o usuário altera o valor de uma fonte de renda a partir de um mês
- **THEN** os meses anteriores mantêm o valor antigo e os seguintes passam a usar o novo

### Requirement: Orçamento mensal por categoria

O usuário SHALL poder definir, para cada mês e categoria, um valor planejado de gasto.

#### Scenario: Orçamento definido
- **WHEN** o usuário define R$ 1.200,00 para Alimentação em agosto
- **THEN** o sistema passa a comparar esse valor com o total de competência dos
  lançamentos de Alimentação em agosto

#### Scenario: Categoria sem orçamento
- **WHEN** existem lançamentos em uma categoria sem valor planejado no mês
- **THEN** os lançamentos são apresentados como fora do planejamento, sem impedir o
  registro

#### Scenario: Valor inválido
- **WHEN** o usuário informa um valor planejado negativo
- **THEN** o sistema recusa a gravação

### Requirement: Reaproveitamento do mês anterior

O sistema SHALL permitir copiar o orçamento de um mês para o mês seguinte.

#### Scenario: Cópia do mês anterior
- **WHEN** o usuário pede para copiar o orçamento de julho para agosto
- **THEN** todos os valores planejados por categoria são replicados em agosto

#### Scenario: Mês de destino já possui orçamento
- **WHEN** o usuário copia para um mês que já tem valores definidos
- **THEN** o sistema pede confirmação antes de substituí-los

### Requirement: Consistência entre renda e orçamento

O sistema SHALL avisar quando a soma dos valores planejados do mês exceder a renda
prevista do mês, sem impedir a operação.

#### Scenario: Orçamento acima da renda
- **WHEN** a soma planejada de agosto é R$ 6.800,00 e a renda prevista é R$ 6.000,00
- **THEN** o sistema exibe aviso informando a diferença e permite salvar

### Requirement: Orçamento pertence ao tenant

O orçamento SHALL ser definido no escopo de um tenant e SHALL ser visível em modo de
leitura por quem recebeu compartilhamento daquele tenant.

#### Scenario: Visualização por terceiro
- **WHEN** Bruno abre o tenant compartilhado da Ana
- **THEN** ele vê o orçamento dela em modo de leitura e não consegue alterá-lo
