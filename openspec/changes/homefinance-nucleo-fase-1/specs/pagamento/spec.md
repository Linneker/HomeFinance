## Purpose

Recebe e valida a contratação e a renovação da assinatura por múltiplos canais de
pagamento, mantendo a situação da assinatura fiel ao que o provedor confirmou.

## ADDED Requirements

### Requirement: Canais de pagamento suportados

O sistema SHALL aceitar contratação por compra in-app das lojas de aplicativo como canal
principal, e por Mercado Pago e Asaas como canais secundários, registrando em cada
assinatura o canal de origem.

#### Scenario: Contratação por compra in-app
- **WHEN** o usuário conclui uma compra in-app na loja
- **THEN** o sistema registra a assinatura com o canal da loja correspondente

#### Scenario: Contratação por canal secundário
- **WHEN** o usuário conclui o pagamento por Mercado Pago ou Asaas
- **THEN** o sistema registra a assinatura com esse canal

#### Scenario: Canal indisponível
- **WHEN** o canal escolhido está indisponível no momento da contratação
- **THEN** o sistema informa a indisponibilidade e mantém a assinatura no estado anterior

### Requirement: Validação no servidor

O sistema SHALL confirmar toda contratação e renovação junto ao provedor antes de conceder
qualquer direito, e SHALL NOT conceder direito com base em informação enviada pelo
aplicativo cliente.

#### Scenario: Comprovante validado
- **WHEN** o aplicativo informa uma compra e a validação junto ao provedor confirma
- **THEN** o sistema ativa a assinatura

#### Scenario: Comprovante não confirmado pelo provedor
- **WHEN** o aplicativo informa uma compra que o provedor não confirma
- **THEN** o sistema não concede direito algum e registra a tentativa

#### Scenario: Comprovante já utilizado
- **WHEN** um comprovante já vinculado a uma conta é apresentado por outra
- **THEN** o sistema recusa a ativação

### Requirement: Notificações do provedor

O sistema SHALL receber e processar as notificações de mudança de estado enviadas pelos
provedores, atualizando a assinatura correspondente.

#### Scenario: Renovação confirmada
- **WHEN** o provedor notifica a renovação de uma assinatura
- **THEN** o sistema estende o período pago e mantém a assinatura ativa

#### Scenario: Cancelamento pelo usuário na loja
- **WHEN** o provedor notifica que o usuário cancelou a renovação
- **THEN** o sistema mantém a assinatura ativa até o fim do período já pago e depois a
  encerra

#### Scenario: Reembolso ou estorno
- **WHEN** o provedor notifica reembolso ou estorno
- **THEN** o sistema suspende a assinatura

#### Scenario: Notificação repetida
- **WHEN** a mesma notificação é recebida mais de uma vez
- **THEN** o sistema a processa uma única vez, sem alterar o estado nas repetições

### Requirement: Reconciliação periódica

O sistema SHALL verificar periodicamente junto aos provedores a situação das assinaturas
vigentes, para corrigir divergências causadas por notificações não entregues.

#### Scenario: Notificação perdida
- **WHEN** uma renovação ocorreu no provedor mas a notificação não foi recebida
- **THEN** a reconciliação identifica a divergência e atualiza a assinatura

#### Scenario: Assinatura encerrada sem aviso
- **WHEN** a reconciliação encontra assinatura encerrada no provedor e ativa no sistema
- **THEN** o sistema a suspende

### Requirement: Dados de cartão não trafegam pela aplicação

O sistema SHALL NOT receber, processar ou armazenar dados de cartão, mantendo apenas os
identificadores devolvidos pelo provedor.

#### Scenario: Tentativa de envio de dados de cartão
- **WHEN** uma requisição inclui dados de cartão
- **THEN** o sistema a recusa

#### Scenario: Referência do provedor armazenada
- **WHEN** uma contratação é concluída em canal secundário
- **THEN** o sistema guarda apenas os identificadores da assinatura e da transação no
  provedor

### Requirement: Vínculo da compra à conta do aplicativo

O sistema SHALL vincular a assinatura à conta do aplicativo, e não à identidade do usuário
no provedor.

#### Scenario: Troca de conta na loja
- **WHEN** o usuário passa a usar outra conta de loja mantendo a mesma conta no aplicativo
- **THEN** a assinatura vigente permanece vinculada à conta do aplicativo

#### Scenario: Mesma conta de loja em outra conta do aplicativo
- **WHEN** a mesma conta de loja tenta ativar assinatura em uma segunda conta do
  aplicativo
- **THEN** o sistema recusa a ativação
