## Context

Repositório greenfield: não existe solução .NET, banco nem cliente. Esta change cria tudo
do zero seguindo o blueprint Acme 2.1.0. Ver `proposal.md` para a motivação e os
`specs/` para os requisitos.

Duas restrições moldam quase todas as decisões abaixo:

1. **SQL puro, sem ORM.** Não existe filtro global de query para injetar `tenant_id`. O
   isolamento precisa vir de outro lugar que não a disciplina de quem escreve o SELECT.
2. **Competência é a visão primária.** Isso não é preferência de relatório: obriga o
   modelo de dados a carregar duas datas em todo lançamento desde a primeira migration.
   Introduzir a segunda data depois exigiria reprocessar todo o histórico dos clientes.

## Goals / Non-Goals

**Goals:**
- Isolamento entre tenants garantido pelo banco, não pela aplicação.
- Modelo de lançamento que suporte competência e caixa sem duplicar registro nem exigir
  cálculo especial por tipo de despesa.
- Estruturas que absorvam o rateio da fase futura sem refatorar consultas de relatório.
- Aderência ao blueprint Acme suficiente para o script `validar_estrutura.py` passar.

**Non-Goals:**
- Otimização de desempenho além de índices óbvios. O volume da Fase 1 é baixo.
- Réplica de leitura real. A separação Read/Write existe no código conforme o blueprint,
  mas ambas as conexões podem apontar para a mesma instância nesta fase.
- Extração e processamento de documentos. MinIO e RabbitMQ entram em uso nesta fase apenas
  para a exportação de dados (arquivo temporário e geração assíncrona); o GED de documentos
  do usuário é Fase 2.

## Decisions

### D1 — Estrutura da solução conforme o blueprint

`Acme.Sistemas.HomeFinance.Api` e `Acme.Sistemas.HomeFinance.App`, mais os projetos
compartilhados sem o nome do sistema (`Services`, `Core`, `Domain`, `Infrastructure`,
`Repository`, `ExternalIntegration`), nas três pastas de topo `infra/`, `src/`, `test/`.
Versionamento por `V1/`. Toda ação com o quinteto CQRS completo — Command, Handler,
Behavior, Result, Validation.

*Alternativa considerada:* estrutura enxuta, dado o tamanho do produto. Recusada porque a
previsibilidade entre projetos Acme é o objetivo declarado do blueprint, e divergir aqui
custaria mais na revisão do que economiza agora.

### D2 — Isolamento por Row Level Security, com tenant ativo na transação

Toda tabela com dado de tenant recebe `ENABLE ROW LEVEL SECURITY` **e**
`FORCE ROW LEVEL SECURITY` — sem o `FORCE`, o dono da tabela ignora as policies e o
isolamento não vale para as conexões da aplicação.

A aplicação usa um role dedicado, sem `BYPASSRLS`. No início de cada transação são
definidos dois parâmetros de sessão:

- `app.tenant_id` — o tenant **selecionado** na requisição, que pode ser o do próprio
  usuário ou um compartilhado com ele.
- `app.readonly` — verdadeiro quando o tenant selecionado não pertence ao usuário.

As policies ficam assim:

- Leitura: permitida quando `tenant_id = app.tenant_id`.
- Escrita (INSERT/UPDATE/DELETE): permitida quando `tenant_id = app.tenant_id`
  **e** `app.readonly` é falso.

Com isso, o compartilhamento somente leitura da spec `tenants-e-compartilhamento` é
imposto pelo banco, não por verificação espalhada nos handlers. A aplicação continua
responsável por validar que o usuário pode selecionar aquele tenant — essa checagem é
feita uma vez, antes de definir os parâmetros.

*Ponto de atenção com pooling:* usar `SET LOCAL` dentro da transação, nunca `SET`. Um
`SET` persiste na conexão devolvida ao pool e vazaria o tenant para a próxima requisição.

**A aplicação nunca conecta como dono do banco.** O usuário criado por `POSTGRES_USER` é
superusuário, e superusuário ignora RLS **mesmo com `FORCE`** — sem erro, sem aviso, todas as
linhas visíveis. O isolamento só existe quando a conexão usa o role `homefinance_app`
(`NOSUPERUSER NOBYPASSRLS`). Isso foi confirmado na implementação: com a conexão de dono, os
seis testes de isolamento passavam com dados de outros tenants à vista.

**Existe um terceiro parâmetro de sessão: `app.usuario_id`.** As tabelas de conta (`usuario`,
`consentimento`, `tenant`, `tenant_compartilhamento`) são lidas *antes* de haver tenant
selecionado — na autenticação e na montagem da lista de tenants disponíveis — e por isso não
podem depender de `app.tenant_id`. Sem esse parâmetro elas ficariam sem RLS, e um `WHERE`
esquecido ali exporia e-mails e a rede de compartilhamentos.

*Alternativa considerada:* schema por tenant. Recusada — para um produto de consumo com
muitos tenants pequenos, multiplicar schemas inviabiliza migrations.

### D3 — Parcela é lançamento irmão, não filho

Uma compra parcelada gera **N lançamentos**, todos com a mesma data de competência (a
data da compra) e datas de caixa distintas (os vencimentos), ligados por um
`grupo_parcelamento_id` usado só para exibição ("3/10").

Isso faz a agregação por competência somar corretamente o valor integral no mês da compra
e a agregação por caixa distribuir as parcelas, **sem lógica condicional por tipo de
lançamento** — as duas visões são o mesmo `GROUP BY` sobre colunas diferentes.

*Alternativa considerada:* lançamento pai com filhos de caixa. Recusada porque
introduziria dois tipos de linha e obrigaria todo relatório a saber qual somar.

### D4 — Participação como tabela desde a Fase 1

`lancamento_participacao(lancamento_id, pessoa_id, percentual)`, com exatamente uma linha
a 100% nesta fase. Todos os relatórios apuram valor por pessoa a partir dessa tabela, e
não do `tenant_id` do lançamento.

O custo é um JOIN a mais em consultas que hoje não precisariam dele. O retorno é que a
introdução do tenant compartilhado com rateio vira inserção de linhas, sem tocar em
nenhuma consulta de relatório. Como a decisão do produto é adiar o rateio mas fazê-lo,
pagar o JOIN agora é mais barato que reescrever depois com clientes em produção.

### D5 — Recorrência materializada em horizonte fixo

Lançamentos recorrentes geram ocorrências previstas materializadas em um horizonte de 12
meses, marcadas como previstas até serem confirmadas. Alterar ou encerrar a regra
regenera as ocorrências futuras ainda não confirmadas.

Isso é o que torna o alerta de caixa possível: projetar saldo exige que as saídas futuras
existam como linhas consultáveis. Calcular recorrência sob demanda em toda projeção seria
mais elegante e bem mais caro.

### D6 — Origem da classificação registrada no lançamento

Cada lançamento guarda se sua categoria veio de regra ou de atribuição manual. É o que
permite cumprir o requisito de que regra nova não sobrescreve classificação manual, sem
manter histórico completo de alterações.

Regras casam por padrão de texto normalizado (maiúsculas, sem acento). Precedência: padrão
mais longo primeiro; empate resolvido pela regra mais recente.

### D7 — Onboarding como Behavior no pipeline

O bloqueio de ações é um `CommandBehavior` aplicado aos Commands que escrevem dado
financeiro, não uma checagem repetida em cada Handler. Requisitos ficam em tabela de
configuração com estado por tenant, cada um com as marcas *declarado* e *ativo* — assim os
requisitos documentais da Fase 2 já aparecem na barra de progresso sem bloquear nada
enquanto não houver como cumpri-los.

Queries não passam pelo Behavior, o que é justamente o que mantém a navegação liberada
desde o primeiro acesso.

### D8 — Alertas com chave de condição e ciclo de vida

Alerta é uma linha com chave de condição (tenant, tipo, categoria, mês de referência) e
estado: aberto, encerrado ou dispensado. Reavaliação disparada por evento de lançamento e
por rotina diária para os alertas de caixa, que dependem da passagem do tempo.

A chave de condição é o que satisfaz o requisito de não repetir o mesmo alerta: reavaliar
uma condição já aberta não cria linha nova.

### D9 — Tipos e datas

Valores monetários em `numeric(18,2)`, nunca ponto flutuante. Competência e caixa como
`date`, não `timestamp` — são datas de calendário e carregar fuso horário nelas só
produziria erro de virada de mês.

### D10 — Clientes desta fase

O app MAUI é a única superfície do usuário final e por isso é **obrigatório na Fase 1**:
sem ele não há produto entregável, apenas API. O portal React não é alternativa a ele — é
back-office administrativo, com público, perfil de acesso e escopo de dados diferentes.

O back-office entra nesta fase apenas no mínimo necessário para operar o produto
(autenticação de administrador e consulta de contas). Sua construção completa acompanha a
capacidade de assinatura e cobrança, que não pertence a esta change.

### D11 — Administrador não é tenant e não vê dado financeiro

A identidade administrativa é separada da identidade do usuário final: administrador não
possui tenant e, portanto, `app.tenant_id` não é definido em suas requisições. Pelas
policies da decisão D2, isso significa que **as consultas de administrador retornam zero
linhas nas tabelas de dado financeiro** — o isolamento já construído para separar clientes
entre si separa também a operação dos clientes, sem mecanismo adicional.

O back-office opera sobre dados de conta e de contrato (identificação, situação, datas),
nunca sobre lançamentos, orçamento ou relatórios. Qualquer necessidade futura de suporte
com acesso a dado financeiro exige consentimento do titular, janela de tempo limitada e
registro em auditoria — e seria mudança de spec, não decisão de implementação.

*Alternativa considerada:* administrador como um tenant especial com visão global.
Recusada — transformaria o mecanismo que garante privacidade no mecanismo que a viola, e
um erro de configuração passaria a expor todos os clientes de uma vez.

### D12 — Direito de acesso é interno, canal de pagamento é detalhe

A situação da assinatura é a única fonte consultada para decidir o que a conta pode fazer.
Ela é mantida pelo próprio sistema; o canal (loja, Mercado Pago, Asaas) fica registrado
como origem e como referência para reconciliação, mas nenhuma decisão de acesso consulta o
provedor em tempo de requisição.

Sem isso, cada canal novo espalharia condicionais por toda a aplicação, e uma instabilidade
do provedor derrubaria o acesso de quem está em dia.

### D13 — Validação sempre no servidor, com reconciliação além das notificações

Comprovante de compra vindo do aplicativo nunca concede direito por si só: é validado
junto ao provedor. As mudanças de estado chegam por notificação do provedor, processadas
de forma idempotente.

Notificação, porém, é entrega não garantida — provedor fora do ar, endpoint indisponível,
mensagem perdida. Por isso existe também reconciliação periódica das assinaturas vigentes.
Depender só de notificação produz o pior defeito possível nessa área: cliente que pagou e
está bloqueado, ou cliente que cancelou e segue usando.

### D14 — Loja é autoritativa sobre o preço no canal in-app

No canal de compra in-app, o catálogo e o valor cobrado são definidos na loja; a API
mantém o catálogo de planos e o vínculo entre produto da loja e plano, mas **não** consegue
alterar em tempo de execução o valor que a loja cobra.

Isso qualifica a intenção de "valores de assinatura geridos pela API": ela é integralmente
válida para Mercado Pago e Asaas, e nos canais de loja se limita ao vínculo e às cotas do
plano. Preço de produto in-app muda na loja, com o prazo de propagação dela.

### D15 — Assinatura restringe recursos derivados, não a entrada de dados

Onboarding e assinatura restringem coisas diferentes, e por isso são dois Behaviors
distintos no pipeline, não um só parametrizado:

```
   ONBOARDING            →  restringe ENTRADA (Commands de dado financeiro)
   ASSINATURA SUSPENSA   →  restringe SAÍDA DERIVADA
                            (análises, alertas, orientações,
                             compartilhamento, processamento de documento)
```

Na suspensão o usuário continua registrando lançamentos e lendo os próprios registros. O
que cessa é o valor produzido sobre esses dados. Isso mantém a série histórica contínua —
uma base com buraco de três meses perde utilidade justamente para as análises que a
assinatura paga — e mantém o produto útil de retomar em vez de exigir recadastramento.

Leitura dos próprios registros no aplicativo e exclusão de conta ficam fora de ambos os
Behaviors, por decisão de arquitetura e não por configuração: são direitos do titular sob
a LGPD e não podem depender de pagamento.

A exportação de dados em arquivo existe, mas nasce desligada e é habilitada conta a conta
pela operação — ver D18.

*Alternativa considerada:* bloquear escrita na suspensão. Recusada — interrompe a série
histórica e transforma a retomada em trabalho de recuperação, sem ganho de conversão sobre
o modelo adotado.

### D17 — Processamento de documento é retido, não descartado

Documento enviado durante a suspensão é armazenado e marcado como pendente; o
processamento (extração por OCR, a partir da Fase 2) só executa com assinatura ativa, e a
fila retida é processada na regularização.

Reter em vez de recusar preserva o dado e cria um incentivo concreto e visível à
regularização. Reter em vez de processar evita custo de extração por conta inadimplente,
que é onde o custo variável do produto se concentra.

Como a extração só existe a partir da Fase 2, esta fase entrega a retenção e a marcação;
a execução da fila entra junto com a capacidade de processar.

### D16 — Assinatura vincula-se à conta do aplicativo

A assinatura pertence à conta do aplicativo, nunca à identidade do usuário na loja. Sem
isso, trocar de conta na loja faria a pessoa perder o que pagou, e uma mesma conta de loja
poderia habilitar várias contas do produto.

### D18 — Exportação construída agora, desligada por padrão, liberada conta a conta

O recurso de exportação em múltiplos formatos é implementado nesta fase, mas nasce
desligado, em dois níveis distintos:

```
   NÍVEL 1 — feature flag global      featureflags.json + FeatureFlagService
             o recurso existe ou não     (mecanismo do blueprint)
             no produto                  interruptor geral, mudança de configuração

   NÍVEL 2 — habilitação por conta    ação de administrador autenticado
             quem pode usar              NO PORTAL ADMINISTRATIVO, com motivo
                                         é dado auditado, não configuração
```

A distinção importa: o nível 2 **não** é edição de arquivo, variável de ambiente nem
acesso direto ao banco. É ação com autor identificado, motivo obrigatório e registro em
auditoria — porque o que se está autorizando é a extração dos dados financeiros de uma
pessoa, e precisa haver resposta para "quem liberou, quando e por quê".

Construir agora e deixar desligado inverte a ordem do problema: quando chegar uma
requisição formal com prazo legal correndo, a resposta é uma mudança de configuração, não
um desenvolvimento. Ao mesmo tempo, a base não fica exposta por padrão, que era a
preocupação de produto.

Três consequências que precisam ser explícitas:

- **A habilitação prevalece sobre a restrição de assinatura.** Liberar exportação para uma
  conta suspensa e deixar o bloqueio de assinatura impedi-la anularia o propósito da
  liberação, que é justamente atender a quem pode ter parado de pagar.
- **A geração roda sob o contexto do próprio titular**, não do administrador. O
  administrador liga a chave; ele não lê o conteúdo. Isso preserva a D11 sem exceção.
- **Habilitação, solicitação e download são auditados.** Sem esse registro não há como
  demonstrar o atendimento à requisição — que é o motivo de o recurso existir.

*Alternativa considerada:* deixar para uma fase futura e atender requisições por
procedimento manual. Recusada — procedimento manual sobre banco de produção para extrair
dado financeiro de um cliente é exatamente o acesso que a D11 existe para impedir.

### D19 — Repositório de escrita também lê, e o contexto de tenant vale nas duas conexões

Conforme o blueprint 2.1.0, o `WriteRepository` expõe também os SELECTs de que o próprio
Command precisa — unicidade, carga antes de alterar, versão para concorrência — sempre
pela `WriteConnection`. `CommandHandler` nunca recebe `I<Entidade>ReadRepository`.

Isso tem uma consequência direta sobre a D2 que precisa estar explícita: **o
`SET LOCAL app.tenant_id` e o `SET LOCAL app.readonly` são aplicados na abertura de
transação de ambas as conexões**, não só na de leitura. Um SELECT de unicidade rodando na
`WriteConnection` sem contexto de tenant definido cairia na policy e retornaria vazio — o
Command concluiria que o registro não existe e gravaria duplicata. O modo de falha aqui é
silencioso e grava dado errado, o que o torna pior que um erro.

Nesta fase as duas conexões podem apontar para a mesma instância, então o defeito não
apareceria em teste. Por isso a convenção nasce agora: quando a `ReadConnection` for
apontada para uma réplica, nenhum Command precisará ser revisado.

## Risks / Trade-offs

- **`SET` em vez de `SET LOCAL` vaza tenant entre requisições no pool** → teste de
  integração que executa duas requisições de tenants diferentes na mesma conexão e exige
  isolamento; revisão obrigatória de qualquer código que toque no contexto de conexão.
- **Role da aplicação com privilégio de dono ignora as policies** → `FORCE ROW LEVEL
  SECURITY` em todas as tabelas e role de aplicação separado, verificado por teste que
  tenta leitura cruzada e espera zero linhas.
- **Tabela nova criada sem policy passa despercebida** → teste que varre o catálogo do
  PostgreSQL e falha se existir tabela com coluna `tenant_id` sem RLS habilitada.
- **Competência confunde o usuário acostumado a caixa** → a visão ativa é sempre rotulada
  na tela, e a alternância fica visível, não escondida em configuração.
- **Recorrência materializada gera volume e pode divergir da regra** → horizonte limitado
  a 12 meses e regeneração idempotente das ocorrências ainda não confirmadas.
- **Onboarding bloqueante pode travar o produto** se os requisitos documentais forem
  ativados antes da Fase 2 existir → requisitos nascem *declarados mas inativos*, e a
  ativação é mudança de configuração explícita.
- **JOIN de participação em toda consulta de relatório** custa desempenho que hoje não é
  necessário → aceito conscientemente; o volume da Fase 1 não torna isso relevante e a
  alternativa é refatorar com clientes em produção.
- **Notificação de provedor perdida deixa cliente pago e bloqueado** → reconciliação
  periódica das assinaturas vigentes, independente das notificações.
- **Notificação duplicada altera estado mais de uma vez** → processamento idempotente por
  identificador da notificação.
- **Suspensão do dono derrubar quem paga em dia** → a suspensão atinge apenas os acessos
  concedidos por ele; o tenant próprio do destinatário permanece operante, com teste
  cobrindo exatamente esse caso.
- **Bloqueio por inadimplência virar retenção indevida de dados** → leitura no aplicativo e
  exclusão de conta ficam fora do Behavior de bloqueio, por decisão de arquitetura e não
  por configuração que alguém possa inverter.
- **Feature flag de exportação ligada por engano de forma global** → a habilitação é por
  conta e exige motivo registrado; teste que confirma recusa quando a conta não está
  habilitada, mesmo com a flag global presente.
- **Arquivo de exportação acessível por quem não solicitou** → download vinculado à conta
  solicitante, com prazo de expiração e remoção do objeto ao fim do prazo.
- **Comprovante de compra reaproveitado entre contas** → vínculo do comprovante à conta do
  aplicativo e recusa de reutilização.
- **Três integrações de pagamento na primeira fase** ampliam a superfície de erro e de
  segredos → credenciais no OpenBao, e a implementação segue a ordem loja → Mercado Pago →
  Asaas, para que cada canal entre com o modelo interno já estável.

## Migration Plan

Produto novo, sem dados legados e sem usuários — não há rollback de dados a planejar. O
que importa é a ordem:

1. Primeira migration cria as tabelas **já com RLS habilitada e forçada**. Habilitar
   depois deixaria uma janela em que dados entram sem proteção.
2. Role de aplicação criado na mesma migration, sem `BYPASSRLS`.
3. Migrations seguintes seguem `Databases/Migrations/` em `Acme.Sistemas.Infrastructure`,
   versionadas e idempotentes.

Rollback nesta fase é recriar o banco a partir das migrations.

## Open Questions

- Prazo exato de retenção de dados após exclusão de conta. Não altera specs nem modelo —
  é parâmetro de configuração e texto de política de privacidade.
- Se a rotina diária de alertas de caixa roda via RabbitMQ ou agendador simples. Ambos
  atendem; a escolha pode esperar a implementação da capability de alertas.
