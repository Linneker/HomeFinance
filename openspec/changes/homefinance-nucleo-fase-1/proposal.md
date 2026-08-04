## Why

O HomeFinance não existe ainda: não há código de aplicação, banco, nem solução .NET.
Esta é a primeira change do produto e estabelece o núcleo sobre o qual todas as fases
seguintes se apoiam — o modelo de lançamento em regime de competência, o isolamento
multi-tenant e o ciclo orçamento → gasto → alerta.

A ordem importa. As fases seguintes (documentos/OCR, crédito, aconselhamento) só têm
onde se encaixar depois que existir um lançamento canônico com dupla data, um tenant
isolado no banco e um orçamento contra o qual comparar. Construir qualquer uma delas
antes obrigaria a refazer o núcleo.

## What Changes

- **Cria a solução .NET** seguindo o blueprint Acme, com a infraestrutura mínima:
  PostgreSQL, migrations, observabilidade, containerização local.
- **Identidade e conta de usuário**, com autenticação e o registro de consentimento
  exigido pela LGPD.
- **Tenant individual, 1:1 com pessoa**, isolado por Row Level Security no PostgreSQL.
  Toda tabela com dado de tenant recebe policy, e há teste automatizado que tenta
  leitura cruzada e espera zero linhas.
- **Compartilhamento de tenant**: assimétrico (Ana compartilhar com Bruno não dá a Ana
  acesso ao tenant do Bruno), somente leitura, revogável a qualquer momento, executável
  apenas pelo dono, precedido de tela de consentimento que lista item a item o que
  ficará visível. Nenhuma agregação: o destinatário troca de visão, não soma valores.
- **Lançamento canônico com duas datas obrigatórias** — competência e caixa. Competência
  é a visão primária dos relatórios; caixa é a secundária, alternável pelo usuário.
- **Estruturas de dados preparadas para rateio** desde já (participação do lançamento
  como tabela, com uma única linha a 100% nesta fase), para que a introdução do tenant
  compartilhado em fase futura não exija refatorar os relatórios.
- **Planejamento orçamentário mensal** por categoria, com renda declarada pelo usuário.
- **Classificação de lançamentos** por regras, em que a correção manual do usuário passa
  a valer como regra para as próximas ocorrências.
- **Relatórios mensais**: extrato detalhado indicando, por lançamento, a qual previsão
  orçamentária ele pertence — ou sinalizando que está fora do planejamento — e resumo
  consolidado por categoria.
- **Alertas de dois tipos, que não se unificam**: competência (estourou o orçamento da
  categoria) e caixa (não haverá saldo na data de vencimento).
- **Onboarding progressivo**: motor de destravamento por níveis com barra de progresso.
  O usuário navega e visualiza o app desde o primeiro acesso, mas só executa ações
  depois de cumprir os requisitos bloqueantes.
- **Assinatura e pagamento**: catálogo de planos com cota de compartilhamento e situação da
  assinatura. Na suspensão, o registro de lançamentos, a leitura dos próprios registros no
  aplicativo e a exclusão de conta permanecem disponíveis; o que cessa são os recursos
  derivados — análises, alertas, orientações, compartilhamento e processamento de
  documentos, que ficam retidos até a regularização. Contratação por compra in-app como
  canal principal e por Mercado Pago e Asaas como secundários, com validação no servidor.
- **Exportação de dados sob feature flag**: implementada nesta fase em múltiplos formatos,
  porém desligada por padrão. A operação habilita conta a conta quando o titular formaliza
  uma requisição de acesso, com motivo registrado, e o titular é avisado. A habilitação
  prevalece sobre a restrição de assinatura, a geração roda sob o contexto do próprio
  titular — nenhum administrador lê o conteúdo — e habilitação, solicitação e download são
  auditados.
- **Assinatura como pré-requisito do compartilhamento**: compartilhar exige plano com cota
  disponível, e visualizar tenant de terceiro exige assinatura ativa também do
  destinatário. A suspensão do dono suspende os acessos que ele concedeu, sem afetar o
  tenant próprio de quem recebia.

### Decisão de escopo sobre o onboarding

Os requisitos bloqueantes definidos para o produto são: renda declarada, Registrato dos
últimos 3 meses e faturas detalhadas de cartão dos últimos 3 meses. Boletos de contas
básicas não são bloqueantes.

Os dois últimos dependem de envio e extração de documentos, que é a Fase 2. Portanto
esta change entrega **o motor de destravamento com requisitos configuráveis** e ativa
apenas o requisito que já pode ser satisfeito — renda declarada. Os requisitos
documentais ficam declarados e visíveis na barra de progresso, e passam a ser exigidos
quando a Fase 2 entregar a ingestão. Sem isso, o app nasceria bloqueado sem meio de
desbloquear.

## Capabilities

### New Capabilities
- `identidade-e-acesso`: conta de usuário, autenticação, sessão e registro de
  consentimento LGPD.
- `tenants-e-compartilhamento`: tenant individual, isolamento por RLS, compartilhamento
  assimétrico somente leitura com consentimento itemizado e revogação.
- `lancamentos`: lançamento canônico com dupla data (competência e caixa), categorias,
  recorrência, participação preparada para rateio e detecção de duplicidade.
- `classificacao`: atribuição de categoria por regras, correção manual e promoção da
  correção a regra permanente.
- `planejamento-orcamentario`: renda declarada e orçamento mensal por categoria.
- `relatorios-mensais`: extrato detalhado com vínculo à previsão orçamentária, resumo
  por categoria e alternância entre competência e caixa.
- `alertas`: alerta de competência (estouro de orçamento) e alerta de caixa (falta de
  saldo projetada), com entrega ao aplicativo.
- `onboarding-progressivo`: níveis de destravamento, requisitos configuráveis, barra de
  progresso e bloqueio de ações antes da conclusão.
- `administracao`: identidade administrativa separada da do usuário final, sem tenant e
  sem acesso a dado financeiro de cliente.
- `assinatura`: catálogo de planos com cota de compartilhamento, situação da assinatura,
  bloqueio de escrita na inadimplência e as condições de assinatura que habilitam o
  compartilhamento.
- `pagamento`: contratação e renovação por compra in-app como canal principal e por
  Mercado Pago e Asaas como secundários, com validação no servidor, notificações do
  provedor e reconciliação periódica.
- `exportacao-dados`: exportação dos dados do próprio tenant em múltiplos formatos,
  desligada por padrão e habilitada conta a conta pela operação, com auditoria.

### Modified Capabilities
Nenhuma. `openspec/specs/` está vazio — este é o primeiro conjunto de specs do produto.

## Impact

- **Código**: cria a solução .NET inteira a partir do zero, seguindo a estrutura de
  camadas do blueprint Acme. Nenhum código existente é afetado porque não há nenhum.
- **Banco**: PostgreSQL com SQL puro via `IDataConfiguration`, sem ORM. Primeiro
  conjunto de migrations. Row Level Security ativa em todas as tabelas com dado de
  tenant — decisão irreversível na prática, porque retrofitar isolamento depois de ter
  clientes é migração de risco alto.
- **Clientes**: o app MAUI é a **única** superfície do usuário final — todos os
  lançamentos, relatórios, alertas e o painel são consumidos por ele, e por isso ele é
  obrigatório nesta fase. O portal React é **back-office administrativo**, usado apenas
  pela operação do produto; usuário final não tem acesso a ele. Ambos consomem a mesma
  API, com perfis de acesso distintos.
- **Infraestrutura**: PostgreSQL, OpenBao, OpenTelemetry e Docker desde já. MinIO e
  RabbitMQ ficam provisionados mas sem uso efetivo nesta fase.
- **Irreversível se errado**: a dupla data no lançamento e o modelo de participação.
  Ambos são baratos agora e caros depois — daí entrarem já nesta fase, mesmo com o
  rateio adiado.

### Fora de escopo
- Fase 2 — envio e extração de documentos (GED no MinIO, Registrato, OCR de faturas e
  boletos via Gemini, fila de revisão).
- Fase 3 — crédito: empréstimo, financiamento e consórcio, com saldo devedor,
  amortização e simulações.
- Fase 4 — aconselhamento de crédito.
- Fase futura — tenant compartilhado com lançamento conjunto, rateio e divisão de renda.
- Open Finance: fora do produto, em qualquer fase.
- Back-office administrativo além do mínimo para operar: relatórios gerenciais, gestão de
  suporte e painéis de negócio.
