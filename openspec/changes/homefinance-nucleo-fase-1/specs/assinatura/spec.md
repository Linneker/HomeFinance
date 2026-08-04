## Purpose

Determina o que cada conta pode fazer conforme o plano contratado e a situação de
pagamento, incluindo a cota de compartilhamentos e o bloqueio de escrita na inadimplência.

## ADDED Requirements

### Requirement: Catálogo de planos

O sistema SHALL manter um catálogo de planos, cada um com a cota de compartilhamentos que
concede, e a administração SHALL poder alterá-lo sem alteração de código.

#### Scenario: Plano com cota de compartilhamento
- **WHEN** um plano define cota de 3 compartilhamentos
- **THEN** contas nesse plano podem conceder até 3 compartilhamentos simultâneos

#### Scenario: Plano sem cota de compartilhamento
- **WHEN** um plano define cota zero
- **THEN** contas nesse plano não podem conceder nenhum compartilhamento

### Requirement: Assinatura da conta

Cada conta SHALL ter no máximo uma assinatura vigente, com situação entre ativa, em
carência, suspensa e cancelada.

#### Scenario: Assinatura ativa
- **WHEN** o pagamento de uma conta é confirmado para o período corrente
- **THEN** a assinatura fica ativa e a conta opera normalmente

#### Scenario: Pagamento não confirmado
- **WHEN** o período pago se encerra sem confirmação de renovação
- **THEN** a assinatura entra em carência pelo prazo configurado e, esgotado o prazo, é
  suspensa

#### Scenario: Segunda assinatura na mesma conta
- **WHEN** uma conta com assinatura vigente tenta contratar outra
- **THEN** o sistema recusa a contratação

### Requirement: Registro de dados preservado na suspensão

O sistema SHALL permitir, durante a suspensão da assinatura, o registro e a alteração de
lançamentos, a leitura dos próprios registros no aplicativo e a exclusão da conta.

#### Scenario: Lançamento com assinatura suspensa
- **WHEN** o usuário com assinatura suspensa registra um lançamento
- **THEN** o lançamento é gravado normalmente

#### Scenario: Leitura dos próprios registros preservada
- **WHEN** o usuário com assinatura suspensa abre o extrato detalhado de um mês
- **THEN** o extrato é apresentado com seus lançamentos

#### Scenario: Exclusão de conta preservada
- **WHEN** o usuário com assinatura suspensa solicita excluir a conta
- **THEN** o sistema atende à solicitação

#### Scenario: Exportação de dados não habilitada
- **WHEN** o usuário com assinatura suspensa solicita a exportação dos próprios dados e o
  recurso não está habilitado para a conta
- **THEN** o sistema recusa, e a consulta permanece disponível tela a tela no aplicativo

#### Scenario: Exportação de dados habilitada
- **WHEN** o usuário com assinatura suspensa solicita a exportação e o recurso está
  habilitado para a conta
- **THEN** o sistema gera a exportação, por a habilitação prevalecer sobre a restrição de
  assinatura

### Requirement: Recursos derivados indisponíveis na suspensão

O sistema SHALL suspender os recursos que produzem valor sobre os dados enquanto a
assinatura estiver suspensa, informando ao usuário que dependem de assinatura ativa. São
eles: relatórios analíticos, alertas e notificações, orientações sobre operações,
concessão e manutenção de compartilhamento, e processamento de documentos enviados.

#### Scenario: Relatório analítico indisponível
- **WHEN** o usuário com assinatura suspensa abre o resumo por categoria do mês
- **THEN** o sistema não apresenta a análise e informa que o recurso depende de assinatura
  ativa

#### Scenario: Alertas suspensos
- **WHEN** uma condição que geraria alerta ocorre em um tenant com assinatura suspensa
- **THEN** nenhum alerta é emitido nem entregue ao dispositivo

#### Scenario: Orientação sobre operação indisponível
- **WHEN** o usuário com assinatura suspensa solicita orientação sobre uma operação
- **THEN** o sistema não apresenta a orientação e informa que o recurso depende de
  assinatura ativa

### Requirement: Documentos enviados na suspensão ficam retidos

O sistema SHALL armazenar os documentos enviados durante a suspensão sem processá-los, e
SHALL processá-los quando a assinatura voltar a ativa.

#### Scenario: Documento enviado com assinatura suspensa
- **WHEN** o usuário com assinatura suspensa envia um documento
- **THEN** o documento é armazenado, fica marcado como pendente de processamento e
  nenhum processamento é executado

#### Scenario: Processamento após regularização
- **WHEN** a assinatura volta a ativa e existem documentos retidos
- **THEN** o sistema processa os documentos retidos

### Requirement: Regularização restaura os recursos

O sistema SHALL restabelecer todos os recursos suspensos quando o pagamento for
confirmado, sem exigir ação adicional do usuário além do pagamento.

#### Scenario: Pagamento confirmado
- **WHEN** o pagamento é confirmado após a suspensão
- **THEN** a assinatura volta a ativa e relatórios analíticos, alertas, orientações e
  compartilhamentos voltam a funcionar

### Requirement: Restrições de onboarding e de assinatura são independentes

O sistema SHALL avaliar a restrição de onboarding e a restrição de assinatura de forma
independente, por atuarem sobre conjuntos distintos de recursos, e SHALL informar a
restrição correspondente ao recurso solicitado.

#### Scenario: Onboarding pendente e assinatura suspensa
- **WHEN** o usuário com onboarding incompleto e assinatura suspensa tenta registrar um
  lançamento
- **THEN** o sistema recusa informando o requisito de onboarding pendente, que é o que
  restringe o registro

#### Scenario: Onboarding concluído e assinatura suspensa
- **WHEN** o usuário com onboarding concluído e assinatura suspensa registra um lançamento
  e em seguida abre o resumo por categoria
- **THEN** o lançamento é gravado e o resumo não é apresentado, informando a dependência de
  assinatura ativa

### Requirement: Compartilhamento exige plano com cota disponível

O sistema SHALL permitir conceder compartilhamento apenas quando a assinatura do dono
estiver ativa e houver cota disponível em seu plano.

#### Scenario: Cota disponível
- **WHEN** Ana tem assinatura ativa, plano com cota 3 e um compartilhamento concedido, e
  compartilha com Bruno
- **THEN** o compartilhamento é concedido e a cota utilizada passa a 2 de 3

#### Scenario: Cota esgotada
- **WHEN** Ana já utilizou toda a cota do plano e tenta compartilhar com mais alguém
- **THEN** o sistema recusa e informa que a cota do plano foi atingida

#### Scenario: Assinatura suspensa
- **WHEN** Ana com assinatura suspensa tenta conceder um novo compartilhamento
- **THEN** o sistema recusa a concessão

### Requirement: Suspensão do dono suspende os compartilhamentos concedidos

Quando a assinatura do dono de um tenant for suspensa, o sistema SHALL suspender o acesso
de todos os destinatários àquele tenant, e SHALL restabelecê-lo com a regularização.

#### Scenario: Dono deixa de pagar
- **WHEN** a assinatura da Ana é suspensa
- **THEN** Bruno deixa de visualizar o tenant da Ana

#### Scenario: Tenant próprio do destinatário preservado
- **WHEN** a assinatura da Ana é suspensa e a assinatura do Bruno segue ativa
- **THEN** Bruno continua operando normalmente o próprio tenant, perdendo apenas o acesso
  ao tenant da Ana

#### Scenario: Regularização do dono
- **WHEN** a assinatura da Ana volta a ativa
- **THEN** os compartilhamentos que ela havia concedido voltam a valer, sem necessidade de
  reconcedê-los

### Requirement: Destinatário precisa de assinatura própria ativa

O sistema SHALL exigir assinatura ativa também de quem visualiza um tenant compartilhado.

#### Scenario: Destinatário sem assinatura ativa
- **WHEN** Bruno, com assinatura suspensa, tenta abrir o tenant compartilhado da Ana
- **THEN** o sistema recusa o acesso e informa a situação da assinatura dele

#### Scenario: Ambas as assinaturas ativas
- **WHEN** as assinaturas de Ana e Bruno estão ativas e o compartilhamento está concedido
- **THEN** Bruno visualiza o tenant da Ana normalmente
