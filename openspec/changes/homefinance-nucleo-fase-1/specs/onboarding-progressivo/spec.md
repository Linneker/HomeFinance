## Purpose

Controla o que o usuário pode fazer conforme os insumos que já forneceu, liberando a
navegação desde o primeiro acesso e destravando as ações por níveis, com barra de progresso
que mostra o que ainda falta.

## ADDED Requirements

### Requirement: Navegação liberada desde o primeiro acesso

O sistema SHALL permitir que o usuário navegue por todas as telas do produto antes de
concluir o onboarding, apresentando os estados vazios correspondentes.

#### Scenario: Primeiro acesso
- **WHEN** o usuário autentica pela primeira vez, sem nenhum insumo fornecido
- **THEN** ele consegue abrir as telas de lançamentos, orçamento e relatórios, que exibem
  estado vazio e a indicação do que falta para liberar as ações

### Requirement: Bloqueio de ações antes da conclusão

O sistema SHALL bloquear as ações de escrita sobre dados financeiros enquanto os requisitos
bloqueantes do onboarding não estiverem cumpridos, e SHALL informar qual requisito está
pendente ao recusar.

#### Scenario: Tentativa de lançamento com onboarding pendente
- **WHEN** o usuário tenta criar um lançamento e há requisito bloqueante pendente
- **THEN** o sistema recusa a operação e informa o requisito que falta

#### Scenario: Ações liberadas após conclusão
- **WHEN** o usuário cumpre todos os requisitos bloqueantes ativos
- **THEN** as ações de escrita passam a ser aceitas

### Requirement: Requisitos configuráveis do onboarding

O sistema SHALL tratar os requisitos do onboarding como configuração, permitindo que um
requisito esteja declarado e visível ao usuário sem estar ativo como bloqueio.

#### Scenario: Requisito declarado e ativo
- **WHEN** o requisito de renda declarada está configurado como ativo e bloqueante
- **THEN** ele é exibido na barra de progresso e impede as ações enquanto não for cumprido

#### Scenario: Requisito declarado e ainda não ativo
- **WHEN** um requisito depende de recurso ainda não disponível no produto
- **THEN** ele é exibido na barra de progresso como etapa futura e não bloqueia as ações

### Requirement: Renda declarada como requisito bloqueante

O sistema SHALL exigir a declaração de ao menos uma fonte de renda antes de liberar as
ações de escrita.

#### Scenario: Renda ainda não declarada
- **WHEN** o usuário não cadastrou nenhuma fonte de renda
- **THEN** o requisito aparece como pendente e as ações de escrita permanecem bloqueadas

#### Scenario: Renda declarada
- **WHEN** o usuário cadastra uma fonte de renda mensal
- **THEN** o requisito é marcado como cumprido e a barra de progresso avança

### Requirement: Barra de progresso do onboarding

O sistema SHALL apresentar o progresso do onboarding com o percentual concluído e a lista
dos requisitos pendentes.

#### Scenario: Progresso parcial
- **WHEN** o usuário cumpriu parte dos requisitos configurados
- **THEN** a barra apresenta o percentual concluído e enumera os requisitos que faltam

#### Scenario: Onboarding concluído
- **WHEN** todos os requisitos ativos foram cumpridos
- **THEN** a barra deixa de ser exibida como pendência

### Requirement: Onboarding é por tenant

O estado do onboarding SHALL ser apurado por tenant, e o acesso somente leitura a um tenant
de terceiro SHALL NOT depender do onboarding do destinatário.

#### Scenario: Leitura de tenant compartilhado com onboarding próprio pendente
- **WHEN** Bruno ainda não concluiu o próprio onboarding e a Ana compartilha o tenant dela
  com ele
- **THEN** Bruno consegue visualizar o tenant da Ana normalmente, enquanto suas ações no
  próprio tenant seguem bloqueadas
