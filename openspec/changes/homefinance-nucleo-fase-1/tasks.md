## 1. Fundação da solução

- [x] 1.1 Criar a estrutura de topo `infra/`, `src/`, `test/` e a solução `.slnx`, com pastas virtuais espelhando as físicas
- [x] 1.2 Criar os projetos `Acme.Sistemas.HomeFinance.Api`, `Acme.Sistemas.Services`, `Acme.Sistemas.Core`, `Acme.Sistemas.Domain`
- [x] 1.3 Criar os projetos `Acme.Sistemas.Infrastructure`, `Acme.Sistemas.Repository`, `Acme.Sistemas.ExternalIntegration` e referenciá-los conforme as camadas do blueprint
- [x] 1.4 Criar os projetos de teste `Acme.Sistemas.Services.UnitTest`, `Acme.Sistemas.IntegrationTest`, `Acme.Sistemas.E2ETest` com xUnit, Moq, Bogus e AutoMock
- [x] 1.5 Implementar o Mediator/CQRS próprio em `Core` (contratos de Command, Query, Notification, Handler, Behavior e pipeline) e o `DepencieInjection.cs`
- [x] 1.6 Criar `infra/compose/docker-compose.yml` com PostgreSQL+pgvector, OpenBao, MinIO, RabbitMQ e a stack de observabilidade
- [x] 1.7 Configurar `IDataConfiguration` com as conexões Read e Write resolvidas via OpenBao, sem segredo em `appsettings`
- [x] 1.8 Instrumentar OpenTelemetry na Api (métricas, logs e traces) e validar a coleta subindo o compose
- [x] 1.9 Escrever o `README` de execução local (subir compose, rodar migrations, rodar a Api, rodar os testes) e atualizar o `CLAUDE.md` com os comandos reais de build, execução e teste
- [x] 1.10 `Dockerfile` da Api com usuário sem privilégio, `.dockerignore` e `NuGet.config` do projeto ignorando o feed privado da máquina
- [x] 1.11 Manifestos Kubernetes (`infra/k8s/v1/deployment.yaml`, `kind-config.yaml`) validados em cluster kind real, com probes, limites e securityContext

## 2. Banco: schema base e isolamento por tenant

- [x] 2.1 Criar o mecanismo de migration versionada em `Acme.Sistemas.Infrastructure/Databases/Migrations/` com execução idempotente
- [x] 2.2 Migration: role de aplicação dedicado, sem `BYPASSRLS`, e concessões mínimas
- [x] 2.3 Migration: tabelas `usuario`, `tenant`, `tenant_compartilhamento`, `consentimento`
- [x] 2.4 Migration: tabelas `categoria`, `lancamento`, `lancamento_participacao`, `regra_classificacao`
- [x] 2.5 Migration: tabelas `fonte_renda`, `orcamento_mensal`, `alerta`, `onboarding_requisito`, `onboarding_estado`
- [x] 2.6 Migration: habilitar `ENABLE` e `FORCE ROW LEVEL SECURITY` em toda tabela com `tenant_id`, criando as policies de leitura e de escrita conforme D2
- [x] 2.7 Implementar no `Repository` a abertura de transação que aplica `SET LOCAL app.tenant_id` e `SET LOCAL app.readonly` — em **ambas** as conexões, Read e Write — garantindo que nunca se use `SET` sem `LOCAL`
- [x] 2.8 Estabelecer a convenção de repositório do blueprint 2.1.0: `WriteRepository` expõe escrita e os SELECTs que decidem a escrita, ambos pela `WriteConnection`; `ReadRepository` atende Queries pela `ReadConnection`; `CommandHandler` nunca recebe `I<Entidade>ReadRepository`
- [x] 2.9 Teste de integração: requisição do tenant A não lê nem escreve linha do tenant B, esperando zero linhas e recusa de escrita
- [x] 2.10 Teste de integração: duas requisições de tenants distintos reaproveitando a mesma conexão do pool permanecem isoladas
- [x] 2.11 Teste de integração: SELECT executado pela `WriteConnection` dentro de um Command enxerga as linhas do tenant ativo, provando que o contexto vale nas duas conexões
- [x] 2.12 Teste de integração: varredura do catálogo do PostgreSQL falha se existir tabela com coluna `tenant_id` sem RLS habilitada e forçada
- [x] 2.13 Criar os índices de apoio às consultas por tenant, competência e caixa

## 3. Identidade e acesso

- [x] 3.1 `Domain`: entidades `UsuarioEntity`, `ConsentimentoEntity` e interfaces de repositório
- [x] 3.2 `Repository`: `UsuarioReadRepository` e `UsuarioWriteRepository` com `Query/` separando SELECTs de comandos de escrita
- [x] 3.3 `Services`: quinteto CQRS de `CriarConta` (Command, Handler, Behavior, Result, Validation), provisionando o tenant individual junto
- [x] 3.4 `Services`: quinteto CQRS de `Autenticar`, emitindo token JWT conforme o padrão de segurança do blueprint
- [x] 3.5 `Services`: quinteto CQRS de `RegistrarConsentimento` e a Query que verifica se a versão aceita está vigente
- [x] 3.6 `Services`: quinteto CQRS de `ExcluirConta`, encerrando compartilhamentos e removendo os dados do tenant
- [x] 3.7 `Api`: endpoints `V1` de criação de conta, autenticação, consentimento e exclusão, com Request, Response e Map
- [x] 3.8 Testes unitários dos handlers de identidade, cobrindo e-mail duplicado, senha fora da política, credencial inválida e sessão expirada
- [x] 3.9 Teste de integração: exclusão de conta encerra os compartilhamentos e o destinatário perde o acesso imediatamente

## 4. Tenants e compartilhamento

- [x] 4.1 `Domain`: entidades `TenantEntity` e `TenantCompartilhamentoEntity` com as interfaces de repositório
- [x] 4.2 `Repository`: repositórios Read/Write de tenant e de compartilhamento
- [x] 4.3 `Core`: resolução do tenant ativo da requisição, validando se o usuário pode selecioná-lo e definindo o modo somente leitura
- [x] 4.4 `Services`: quinteto CQRS de `CompartilharTenant`, exigindo confirmação do consentimento itemizado
- [x] 4.5 `Services`: Query que monta a lista de itens do consentimento, marcando as categorias sensíveis como fora do compartilhamento por padrão
- [x] 4.6 `Services`: quinteto CQRS de `RevogarCompartilhamento`, com efeito na requisição seguinte
- [x] 4.7 `Services`: Query de tenants disponíveis para o usuário (o próprio e os compartilhados com ele)
- [x] 4.8 `Api`: endpoints `V1` de compartilhamento, revogação e listagem de tenants disponíveis
- [x] 4.9 Testes unitários cobrindo assimetria do compartilhamento, recusa de repasse por quem só tem leitura e consentimento não confirmado
- [x] 4.10 Teste de integração: destinatário com leitura tem escrita recusada pelo banco, não apenas pela aplicação

## 5. Lançamentos

- [x] 5.1 `Domain`: entidades `CategoriaEntity`, `LancamentoEntity` e `LancamentoParticipacaoEntity`, com enums de tipo e origem
- [x] 5.2 `Repository`: repositórios Read/Write de categoria, lançamento e participação
- [x] 5.3 `Services`: quinteto CQRS de `CriarCategoria` e Query de listagem
- [x] 5.4 `Services`: quinteto CQRS de `CriarLancamento`, exigindo as duas datas e gravando a participação única de 100%
- [x] 5.5 `Services`: geração de parcelas como lançamentos irmãos com mesma competência, caixas distintas e `grupo_parcelamento_id` comum
- [x] 5.6 `Services`: verificação de duplicidade por descrição, valor e competência, devolvendo aviso que exige confirmação
- [x] 5.7 `Services`: quinteto CQRS de `AlterarLancamento` e de `RemoverLancamento`, recalculando o consumo de orçamento afetado
- [x] 5.8 `Services`: recorrência mensal com materialização das ocorrências previstas no horizonte de 12 meses
- [x] 5.9 `Services`: encerramento de recorrência, regenerando de forma idempotente apenas as ocorrências futuras ainda não confirmadas
- [x] 5.10 `Api`: endpoints `V1` de lançamento (criação, alteração, remoção, listagem) e de categoria
- [x] 5.11 Testes unitários: lançamento à vista, consumo em um mês com pagamento em outro, data ausente e categoria inexistente
- [x] 5.12 Testes unitários: compra em três parcelas soma o valor integral na competência do mês da compra e distribui o caixa nos três meses seguintes
- [x] 5.13 Testes unitários: duplicidade detectada exige confirmação e, confirmada, grava normalmente

## 6. Classificação

- [x] 6.1 `Domain`: entidade `RegraClassificacaoEntity` e a interface de repositório
- [x] 6.2 `Repository`: repositórios Read/Write de regra de classificação
- [x] 6.3 `Services`: `ClassificacaoService` com normalização de descrição e precedência por especificidade e recência
- [x] 6.4 `Services`: aplicação das regras na criação do lançamento, marcando como pendente quando nenhuma casar
- [x] 6.5 `Services`: quinteto CQRS de `ClassificarLancamento`, gravando a origem manual da categoria
- [x] 6.6 `Services`: criação opcional de regra a partir da correção manual, e substituição da regra anterior quando a categoria for trocada
- [x] 6.7 `Services`: garantir que aplicação de regra nunca sobrescreve categoria de origem manual
- [x] 6.8 `Services`: quinteto CQRS de alteração e remoção de regra, e Query de listagem
- [x] 6.9 `Api`: endpoints `V1` de classificação e de gestão de regras
- [x] 6.10 Testes unitários: regra casa, nenhuma regra casa, duas regras casam, recusa de criar regra e preservação da classificação manual

## 7. Planejamento orçamentário

- [x] 7.1 `Domain`: entidades `FonteRendaEntity` e `OrcamentoMensalEntity` com as interfaces de repositório
- [x] 7.2 `Repository`: repositórios Read/Write de fonte de renda e de orçamento mensal
- [x] 7.3 `Services`: quinteto CQRS de cadastro, alteração e remoção de fonte de renda, com vigência por mês
- [x] 7.4 `Services`: quinteto CQRS de definição de orçamento mensal por categoria, recusando valor negativo
- [x] 7.5 `Services`: cópia do orçamento do mês anterior, pedindo confirmação quando o destino já tiver valores
- [x] 7.6 `Services`: aviso quando a soma planejada do mês exceder a renda prevista, sem impedir a gravação
- [x] 7.7 `Api`: endpoints `V1` de renda e de orçamento mensal
- [x] 7.8 Testes unitários: renda com duas fontes, alteração de renda preservando meses anteriores, valor planejado negativo e orçamento acima da renda
- [x] 7.9 Teste de integração: destinatário do compartilhamento vê o orçamento e a renda do dono e não consegue alterá-los

## 8. Relatórios mensais

- [x] 8.1 `Repository`: consultas de extrato e de resumo por categoria, agregando sempre a partir de `lancamento_participacao`
- [x] 8.2 `Services`: Query de extrato detalhado do mês, com vínculo à previsão orçamentária e marcação de fora do planejamento
- [x] 8.3 `Services`: destaque dos lançamentos pendentes de classificação no extrato
- [x] 8.4 `Services`: alternância entre competência e caixa, com competência como padrão e a visão ativa explícita no resultado
- [x] 8.5 `Services`: Query de resumo por categoria com planejado, realizado e diferença, mais o agrupamento de gastos sem planejamento
- [x] 8.6 `Api`: endpoints `V1` de extrato e de resumo mensal
- [x] 8.7 Testes unitários: mesma compra parcelada aparece integral em julho na competência e como parcela em agosto no caixa
- [x] 8.8 Testes unitários: categoria dentro do orçamento, categoria estourada, mês sem lançamentos e ausência de soma entre tenants
- [x] 8.9 Teste de integração: destinatário do compartilhamento abre o relatório do dono e vê apenas os valores daquele tenant, nas duas direções da troca

## 9. Alertas

- [x] 9.1 `Domain`: entidade `AlertaEntity` com chave de condição e estados aberto, encerrado e dispensado
- [x] 9.2 `Repository`: repositórios Read/Write de alerta, com busca por chave de condição
- [x] 9.3 `Services`: avaliação do alerta de competência ao criar, alterar, remover ou reclassificar lançamento
- [x] 9.4 `Services`: projeção de saldo por data a partir da renda prevista e das saídas de caixa previstas
- [x] 9.5 `Services`: avaliação do alerta de caixa, emitindo antes da data projetada de insuficiência
- [x] 9.6 `Services`: rotina diária de reavaliação dos alertas de caixa
- [x] 9.7 `Services`: encerramento automático do alerta quando a condição deixa de existir, e dispensa pelo usuário
- [x] 9.8 `Services`: entrega dos alertas apenas ao dono do tenant, nunca a quem tem somente leitura
- [x] 9.9 `Api`: endpoints `V1` de listagem e de dispensa de alerta
- [x] 9.10 Testes unitários: estouro de orçamento, categoria sem orçamento não alerta, estouro corrigido encerra, insuficiência de caixa projetada e ausência de duplicação da mesma condição
- [x] 9.11 Teste unitário: estouro e insuficiência simultâneos produzem dois alertas distintos, nunca um combinado
- [x] 9.12 Teste de integração: índice parcial barra a segunda emissão da condição aberta e libera a chave depois do encerramento; alerta do dono não é entregue nem alterado por quem tem somente leitura

## 10. Onboarding progressivo

- [x] 10.1 `Domain`: entidades `OnboardingRequisitoEntity` e `OnboardingEstadoEntity`, com as marcas declarado e ativo
- [x] 10.2 `Repository`: repositórios Read/Write de requisito e de estado do onboarding
- [x] 10.3 Migration de dados: semear os requisitos, com renda declarada ativo e bloqueante e os requisitos documentais declarados porém inativos
- [x] 10.4 `Services`: `OnboardingBehavior` aplicado aos Commands que escrevem dado financeiro, recusando com a indicação do requisito pendente
- [x] 10.5 `Services`: marcação automática do requisito de renda como cumprido ao cadastrar a primeira fonte de renda
- [x] 10.6 `Services`: Query de progresso com percentual concluído e lista de requisitos pendentes
- [x] 10.7 `Services`: apuração do estado por tenant, sem afetar a leitura de tenant compartilhado por destinatário com onboarding pendente
- [x] 10.8 `Api`: endpoint `V1` de progresso do onboarding
- [x] 10.9 Testes unitários: escrita recusada com requisito pendente, liberação após cumprimento, requisito declarado e inativo não bloqueia
- [x] 10.10 Teste de integração: Query de leitura não passa pelo Behavior e permanece acessível com onboarding pendente

## 11. Assinatura e pagamento

- [x] 11.1 `Domain`: entidades `PlanoEntity`, `AssinaturaEntity` e `TransacaoPagamentoEntity`, com enums de situação e de canal
- [x] 11.2 Migration: tabelas `plano`, `assinatura`, `transacao_pagamento` e `notificacao_provedor`, com as policies adequadas ao escopo de cada uma
- [x] 11.3 Migration de dados: semear o catálogo inicial de planos com as respectivas cotas de compartilhamento
- [x] 11.4 `Repository`: repositórios Read/Write de plano, assinatura e transação
- [x] 11.5 `Services`: `AssinaturaService` resolvendo a situação vigente da conta, única fonte consultada para decisão de acesso
- [x] 11.6 `Services`: transição de situação da assinatura entre ativa, carência, suspensa e cancelada, com prazo de carência configurável
- [x] 11.7 `Services`: Behavior de assinatura restringindo os recursos derivados — análises, alertas, orientações e compartilhamento — independente do Behavior de onboarding e sem alcançar o registro de lançamentos
- [x] 11.8 `Services`: retenção de documento enviado na suspensão, marcado como pendente, e disparo do processamento da fila retida na regularização
- [x] 11.9 `Services`: validação de cota do plano na concessão de compartilhamento e recusa quando esgotada ou assinatura inativa
- [x] 11.10 `Services`: suspensão e restabelecimento em cascata dos compartilhamentos concedidos, preservando o tenant próprio do destinatário
- [x] 11.11 `Services`: exigência de assinatura ativa do destinatário para abrir tenant compartilhado
- [x] 11.12 `ExternalIntegration`: validação de comprovante de compra in-app junto às lojas, com credenciais no OpenBao
- [x] 11.13 `Api`: endpoint de recebimento das notificações das lojas, com processamento idempotente por identificador
- [x] 11.14 `ExternalIntegration` e `Api`: integração e notificações do Mercado Pago
- [x] 11.15 `ExternalIntegration` e `Api`: integração e notificações do Asaas
- [x] 11.16 `Services`: vínculo do comprovante à conta do aplicativo, recusando reutilização em outra conta
- [x] 11.17 `Services`: rotina de reconciliação periódica das assinaturas vigentes junto aos provedores
- [x] 11.18 `Api`: endpoints `V1` de catálogo de planos, situação da assinatura e contratação
- [x] 11.19 Testes unitários: com assinatura suspensa, lançamento é gravado, extrato é lido, exclusão de conta funciona, e resumo por categoria e alertas ficam indisponíveis
- [x] 11.20 Testes unitários: cota esgotada, concessão com assinatura suspensa, destinatário sem assinatura ativa e independência entre as restrições de onboarding e de assinatura
- [x] 11.21 Testes unitários: comprovante não confirmado pelo provedor, comprovante já utilizado e notificação repetida
- [x] 11.22 Teste de integração: suspensão do dono derruba o acesso compartilhado e mantém o tenant próprio do destinatário operante
- [x] 11.23 Teste de integração: regularização restaura análises, alertas e compartilhamentos sem ação adicional do usuário
- [x] 11.24 Teste de integração: reconciliação corrige assinatura divergente quando a notificação não foi entregue

- [x] 11.25 Testes unitários dos adaptadores de pagamento: tradução da resposta de cada provedor, credencial buscada no cofre a cada chamada e nenhum dado de cartão no tráfego — compra in-app (App Store/Google Play) e gateways Mercado Pago/Asaas, com Bearer e cabeçalho próprio
- [x] 11.26 Teste de integração: notificação repetida é processada uma única vez contra o índice `ux_notificacao_provedor`; e o mapa de rótulos do provedor traduz o desconhecido em estorno, nunca em renovação

## 12. Exportação de dados

- [x] 12.1 `Infrastructure`: `featureflags.json` e `FeatureFlagService` conforme o blueprint, com a flag de exportação desligada por padrão
- [x] 12.2 `Domain` e migration: entidades `ExportacaoHabilitacaoEntity` e `ExportacaoSolicitacaoEntity`, com motivo, autor e situação
- [x] 12.3 `Repository`: repositórios Read/Write de habilitação e de solicitação de exportação
- [x] 12.4 `Services` e `Api`: habilitação e desabilitação da exportação por conta, exigindo motivo e registrando autor e data, expostas somente em endpoint de perfil administrativo
- [x] 12.5 `Services`: geração assíncrona da exportação sob o contexto do próprio titular, publicando na fila e gravando o arquivo no MinIO
- [x] 12.6 `Services`: formatadores de saída — ao menos um formato de leitura humana e um de dados estruturados — recusando formato não suportado
- [x] 12.7 `Services`: recusa de exportação de tenant recebido por compartilhamento
- [x] 12.8 `Services`: prevalência da habilitação sobre a restrição de assinatura
- [x] 12.9 `Services`: aviso ao titular na habilitação e na conclusão do arquivo, entregue mesmo com notificações suspensas
- [x] 12.10 `Services`: expiração do arquivo, remoção do objeto no MinIO e download vinculado à conta solicitante
- [x] 12.11 `Services`: registro em auditoria de habilitação, desabilitação, solicitação e download
- [x] 12.12 `Api`: endpoints `V1` de solicitação de exportação, consulta de situação e download
- [x] 12.13 Testes unitários: recusa sem habilitação, habilitação sem motivo, formato não suportado, recusa para destinatário de compartilhamento e prevalência sobre assinatura suspensa
- [x] 12.14 Teste de integração: requisição vinda do perfil de usuário final não consegue habilitar a exportação, em nenhum endpoint
- [x] 12.15 Teste de integração: arquivo expirado é removido e o download passa a ser recusado, e download por outra conta é recusado

## 13. Aplicativo MAUI (superfície do usuário final)

- [ ] 13.1 Criar o projeto `Acme.Sistemas.HomeFinance.App` em `src/App/`, com navegação base e armazenamento seguro do token de sessão
- [ ] 13.2 Fluxo de criação de conta e aceite de termos, registrando o consentimento
- [ ] 13.3 Fluxo de autenticação, renovação e expiração de sessão
- [ ] 13.4 Tela de planos e contratação por compra in-app, com a validação concluída no servidor antes de liberar qualquer recurso
- [ ] 13.5 Contratação pelos canais secundários Mercado Pago e Asaas, sem que dado de cartão passe pela aplicação
- [ ] 13.6 Tela de situação da assinatura, com carência, suspensão e caminho de regularização
- [ ] 13.7 Seletor de tenant, com indicação permanente e inequívoca de modo somente leitura
- [ ] 13.8 Telas de compartilhamento: consentimento itemizado com categorias sensíveis desmarcadas por padrão, lista, cota do plano utilizada e revogação
- [ ] 13.9 Tela de lançamento com as duas datas, parcelamento, recorrência e confirmação de duplicidade
- [ ] 13.10 Telas de renda e de orçamento mensal, com cópia do mês anterior e aviso de orçamento acima da renda
- [ ] 13.11 Extrato detalhado com vínculo à previsão, destaque de pendentes e ação de classificar com opção de criar regra
- [ ] 13.12 Resumo por categoria e alternância entre competência e caixa, com a visão ativa sempre rotulada na tela
- [ ] 13.13 Painel inicial consolidando o mês corrente, alertas abertos e progresso do onboarding
- [ ] 13.14 Central de alertas, com os dois tipos visualmente distintos e ação de dispensar
- [ ] 13.15 Estados vazios de todas as telas antes da conclusão do onboarding, mantendo a navegação liberada
- [ ] 13.16 Estado de assinatura suspensa: registro de lançamento e leitura preservados, recursos derivados exibidos como indisponíveis com motivo claro e caminho de regularização, e exclusão de conta acessível
- [ ] 13.17 Tela de exportação, visível somente quando o recurso está habilitado para a conta, com escolha de formato, acompanhamento da geração e download
- [ ] 13.18 Entrega de alertas ao dispositivo do dono do tenant
- [ ] 13.19 Testes E2E com Appium: criação de conta, contratação, onboarding, primeiro lançamento, classificação com criação de regra e visualização de tenant compartilhado

## 14. Back-office administrativo (mínimo)

- [x] 14.1 `Domain` e `Repository`: identidade administrativa separada da identidade do usuário final, sem vínculo com tenant
- [x] 14.2 `Services`: autenticação de administrador com perfil próprio, isolada do fluxo de usuário final
- [x] 14.3 `Services`: Query de contas com dados de conta, situação da assinatura e plano, sem qualquer dado financeiro
- [x] 14.4 `Services`: manutenção do catálogo de planos e de suas cotas de compartilhamento
- [x] 14.5 `Api`: endpoints `V1` administrativos, segregados por perfil de acesso
- [ ] 14.6 Criar o portal React em `src/Web/portal` com autenticação administrativa, listagem de contas e gestão do catálogo de planos
- [ ] 14.7 Tela administrativa de habilitação e desabilitação da exportação por conta, com motivo obrigatório e histórico das habilitações
- [x] 14.8 Teste de integração: requisição autenticada como administrador retorna zero linhas em todas as tabelas de dado financeiro, inclusive após habilitar exportação para a conta
- [x] 14.9 Teste de integração: credencial de administrador não autentica nos endpoints de usuário final, e vice-versa

## 15. Conformidade e fechamento

- [ ] 15.1 Rodar `validar_estrutura.py` do blueprint Acme e corrigir todo desvio que quebre o padrão
- [ ] 15.2 Revisar o checklist das 26 regras de validação do blueprint e registrar as lacunas de cobertura que ficarem para as fases seguintes
- [ ] 15.3 Conferir que nenhum segredo está versionado em texto claro e que tudo resolve via OpenBao, incluindo as credenciais de loja, Mercado Pago e Asaas
- [ ] 15.4 Publicar os dashboards de saúde no Grafana e validar métricas, logs e traces ponta a ponta
- [ ] 15.5 Rodar a suíte completa (unitário, integração e E2E) e registrar o resultado
- [ ] 15.6 Rodar `openspec validate "homefinance-nucleo-fase-1"` e corrigir pendências antes do arquivamento
