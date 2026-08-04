## Purpose

Define o lançamento, unidade canônica de todo movimento financeiro do produto, com as duas
datas que sustentam as visões de competência e de caixa, além de categorias, recorrência e
proteção contra registro duplicado.

## ADDED Requirements

### Requirement: Dupla data obrigatória

Todo lançamento SHALL registrar uma data de competência e uma data de caixa. Nenhum
lançamento SHALL ser persistido com apenas uma delas.

#### Scenario: Lançamento à vista
- **WHEN** o usuário registra uma compra paga no ato em 03/08
- **THEN** o lançamento é gravado com data de competência 03/08 e data de caixa 03/08

#### Scenario: Consumo em um mês e pagamento em outro
- **WHEN** o usuário registra uma conta de luz referente ao consumo de julho com
  vencimento em 10/08
- **THEN** o lançamento é gravado com competência em julho e caixa em 10/08

#### Scenario: Data ausente
- **WHEN** o usuário tenta salvar um lançamento sem informar uma das duas datas
- **THEN** o sistema recusa a gravação e indica qual data está faltando

### Requirement: Compra parcelada

O sistema SHALL registrar uma compra parcelada com a competência integral na data da
compra e uma data de caixa por parcela.

#### Scenario: Compra em três parcelas
- **WHEN** o usuário registra uma compra de R$ 300,00 em 3 parcelas feita em 20/07, com
  parcelas vencendo em agosto, setembro e outubro
- **THEN** a competência do valor total de R$ 300,00 fica em julho, e são geradas três
  saídas de caixa de R$ 100,00 nos meses de agosto, setembro e outubro

### Requirement: Participação do lançamento

Todo lançamento SHALL ter sua atribuição registrada como participação de pessoa com
percentual, e nesta fase SHALL existir exatamente uma participação de 100% para o dono do
tenant.

#### Scenario: Participação criada com o lançamento
- **WHEN** um lançamento é criado no tenant da Ana
- **THEN** o sistema grava uma participação única atribuindo 100% do valor à Ana

#### Scenario: Relatórios leem a participação
- **WHEN** um relatório apura o valor atribuído a uma pessoa
- **THEN** a apuração é feita a partir das participações do lançamento, e não do
  identificador do tenant

### Requirement: Categoria do lançamento

Um lançamento SHALL poder ter uma categoria, e SHALL poder existir sem categoria enquanto
aguarda classificação.

#### Scenario: Lançamento sem categoria
- **WHEN** um lançamento é criado sem categoria atribuída
- **THEN** ele é persistido e marcado como pendente de classificação

#### Scenario: Categoria inexistente
- **WHEN** o usuário informa uma categoria que não existe no tenant
- **THEN** o sistema recusa a gravação

### Requirement: Lançamento recorrente

O sistema SHALL permitir marcar um lançamento como recorrente mensal, gerando as
ocorrências futuras previstas.

#### Scenario: Conta fixa mensal
- **WHEN** o usuário marca a mensalidade da internet como recorrente mensal
- **THEN** o sistema passa a prever a ocorrência nos meses seguintes, com competência e
  caixa no mesmo dia do mês de origem

#### Scenario: Encerramento da recorrência
- **WHEN** o usuário encerra a recorrência
- **THEN** as ocorrências futuras previstas deixam de ser geradas, e as já realizadas
  permanecem inalteradas

### Requirement: Detecção de lançamento duplicado

Ao gravar um lançamento, o sistema SHALL avisar quando já existir, no mesmo tenant, outro
lançamento com mesma descrição, mesmo valor e mesma competência.

#### Scenario: Possível duplicidade detectada
- **WHEN** o usuário registra um lançamento de R$ 280,00 com competência em julho e já
  existe outro idêntico no tenant
- **THEN** o sistema avisa sobre a possível duplicidade e pede confirmação antes de gravar

#### Scenario: Duplicidade confirmada como legítima
- **WHEN** o usuário confirma que os dois lançamentos são de fato distintos
- **THEN** o sistema grava o segundo lançamento normalmente

### Requirement: Alteração e remoção de lançamento

O usuário SHALL poder alterar ou remover lançamentos do próprio tenant, e as alterações
SHALL refletir imediatamente nos relatórios e no consumo de orçamento.

#### Scenario: Alteração de valor
- **WHEN** o usuário altera o valor de um lançamento já classificado
- **THEN** o consumo do orçamento da categoria correspondente é recalculado no mês de
  competência do lançamento

#### Scenario: Remoção
- **WHEN** o usuário remove um lançamento
- **THEN** ele deixa de compor relatórios e o consumo de orçamento é recalculado
