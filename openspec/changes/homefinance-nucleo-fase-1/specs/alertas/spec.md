## Purpose

Avisa o usuário sobre dois problemas de natureza distinta que não se confundem: o gasto
que ultrapassou o orçamento planejado da categoria, e a falta de saldo projetada para
honrar as saídas previstas de uma data.

## ADDED Requirements

### Requirement: Alerta de competência

O sistema SHALL emitir alerta quando o total realizado em competência de uma categoria
ultrapassar o valor planejado daquele mês.

#### Scenario: Orçamento da categoria excedido
- **WHEN** Transporte tem R$ 400,00 planejados em agosto e a competência realizada atinge
  R$ 420,00
- **THEN** o sistema emite alerta informando a categoria, o mês e o valor excedido

#### Scenario: Categoria sem orçamento definido
- **WHEN** há lançamentos em uma categoria sem valor planejado no mês
- **THEN** nenhum alerta de estouro é emitido para essa categoria

#### Scenario: Estouro corrigido
- **WHEN** o usuário remove ou reclassifica lançamentos e a categoria volta a ficar dentro
  do planejado
- **THEN** o alerta é encerrado

### Requirement: Alerta de caixa

O sistema SHALL emitir alerta quando o saldo projetado para uma data for insuficiente para
cobrir as saídas de caixa previstas até ela, considerando a renda prevista e os
lançamentos com data de caixa no período.

#### Scenario: Saldo projetado insuficiente
- **WHEN** as saídas de caixa previstas até 10/08 somam R$ 3.400,00 e a renda prevista
  disponível até essa data é R$ 2.100,00
- **THEN** o sistema emite alerta indicando a data e a diferença de R$ 1.300,00

#### Scenario: Alerta antecipado
- **WHEN** a insuficiência é projetada para uma data futura
- **THEN** o alerta é emitido antes daquela data, e não no dia em que ela ocorre

#### Scenario: Saldo suficiente
- **WHEN** a renda prevista cobre as saídas de caixa previstas do período
- **THEN** nenhum alerta de caixa é emitido

### Requirement: Alertas não se unificam

O sistema SHALL manter alerta de competência e alerta de caixa como tipos distintos, com
mensagens próprias, e SHALL NOT combiná-los em um único aviso.

#### Scenario: Ambos os alertas ativos
- **WHEN** uma categoria estourou o orçamento e há insuficiência de caixa projetada no
  mesmo mês
- **THEN** o sistema apresenta dois alertas separados, cada um com sua natureza
  identificada

### Requirement: Entrega e ciclo de vida do alerta

O sistema SHALL entregar os alertas ao aplicativo do usuário dono do tenant e SHALL evitar
repetir o mesmo alerta enquanto a condição que o gerou não mudar.

#### Scenario: Alerta entregue ao dono
- **WHEN** um alerta é gerado no tenant da Ana
- **THEN** ele é entregue à Ana

#### Scenario: Alerta não entregue a quem tem apenas leitura
- **WHEN** Bruno tem acesso somente leitura ao tenant da Ana e um alerta é gerado nesse
  tenant
- **THEN** Bruno não recebe o alerta

#### Scenario: Alerta não duplicado
- **WHEN** a mesma condição de estouro permanece após novos lançamentos que não alteram o
  quadro
- **THEN** o sistema não emite um novo alerta para a mesma condição

#### Scenario: Alerta dispensado pelo usuário
- **WHEN** o usuário dispensa um alerta
- **THEN** o alerta deixa de ser exibido e só volta a ser emitido se a condição for
  encerrada e ocorrer novamente
