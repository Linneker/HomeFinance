# Conformidade e fechamento — homefinance-nucleo-fase-1

Registro do grupo 15. Resultado da validação estrutural, das 26 regras do blueprint, da checagem
de segredos, da observabilidade e da suíte de testes, com as lacunas que ficam para fases seguintes.

## 15.1 — `validar_estrutura.py`

Desvios que quebravam o padrão foram corrigidos:

- **Regra 22** — `featureflags.json` passou para a raiz do Api (`src/Api/.../featureflags.json`),
  copiado para a saída; o `FeatureFlagService` lê `AppContext.BaseDirectory/featureflags.json`. A
  cópia antiga em `Infrastructure/FeatureFlags/` foi removida.
- **Regra 10** — repositórios renomeados para a separação leitura/escrita por nome:
  `AuditoriaRepository` → `AuditoriaWriteRepository` (a leitura é `AuditoriaReadRepository`);
  `AdministradorRepository` → `AdministradorWriteRepository` (a leitura é
  `AdministracaoReadRepository`); `DocumentoRetidoRepository` → `DocumentoRetidoWriteRepository`.

### Desvios residuais aceitos (com justificativa)

- **Regra 10 — `DocumentoRetido` sem `ReadRepository`.** É intencional e está documentado na
  interface: as leituras da fila (pendentes) decidem escrita e, por isso, correm pela conexão de
  escrita — ler por réplica atrasada reprocessaria o que já saiu da fila. Não há leitura de usuário
  final sobre a fila na Fase 1 (documentos são Fase 2), então não existe `ReadRepository` legítimo;
  criar um vazio só para satisfazer o verificador seria código morto.
- **Regra 15 — `infra/compose/.env`.** O arquivo é **local e ignorado pelo git** (`.gitignore`),
  criado a partir do `.env.example` para subir a stack de desenvolvimento; contém apenas
  placeholders (`troque_este_valor`), não segredo versionado. O verificador varre o disco e não
  enxerga o `.gitignore`.

## 15.2 — Checklist das 26 regras (lacunas para as próximas fases)

Lacunas de cobertura (não quebram o padrão; ficam planejadas):

- **Regra 12** — endpoints administrativos `AdminContas`, `AdminPlanos` e `ExportacaoAdmin` sem
  `Response.cs`/`Map.cs` dedicados: devolvem os `record` de resultado da Query diretamente. Como já
  são DTOs limpos, a tradução extra foi dispensada; padronizar Request/Response/Map fica para um
  passo de polimento.
- **Regra 4** — pasta `Services/V1/Exportacao/Formatadores`: agrupa os formatadores CSV/JSON/Texto,
  fora do quinteto Command/Query/Event/Services. É coesão legítima do recurso; mantida como está.

Regras `[manual]` não cobertas pelo script (2, 9, 11, 14, 16, 17, 18, 19, 21, 26) — situação:

- **2 (camadas), 9 (SQL puro/`IDataConfiguration`), 11 (segregação leitura/escrita), 14 (OpenBao),
  16 (migrations versionadas), 26 (auditoria funcional)** — atendidas no núcleo implementado.
- **17 (OpenTelemetry), 21 (dashboards)** — instrumentação e dashboards versionados (ver 15.4); a
  validação ponta a ponta depende da stack de observabilidade rodando.
- **18 (Kubernetes)** — manifests em `infra/k8s/v1`; deploy real fora do escopo da Fase 1.
- **19 (AutoProcessPlatform / pipelines)** — fora do escopo da Fase 1.

### Itens diferidos do app (grupo 13)

- **13.18 — push real:** abstraído em `INotificacaoDispositivo` (stub); a entrega por APNs/FCM/WNS e
  o endpoint de registro de dispositivo ficam para quando houver credenciais de push. A central de
  alertas por *pull* cobre o intervalo.
- **13.19 — E2E Appium:** jornadas escritas como `Skip` em `JornadasDoAppTests`; exigem
  emulador/dispositivo e servidor Appium.
- **Alvos móveis do MAUI:** o build fica no Windows (único workload instalado); Android/iOS/
  macCatalyst entram com `-p:IncluirMovel=true` quando os workloads existirem.

## 15.3 — Segredos

Nenhum segredo real versionado em texto claro:

- `appsettings.json` traz só configuração do OpenBao (mount/caminho), nenhum valor de segredo.
- `docker-compose.yml` usa `${VAR:-dev_only_change_me}` — placeholders de desenvolvimento,
  sobrescritos por ambiente.
- `infra/k8s/v1/deployment.yaml` traz o `Secret` com `OPENBAO_TOKEN: "REPLACE_AT_DEPLOY"`
  (placeholder); connection strings e credenciais nunca vivem ali.
- Credenciais de **loja, Mercado Pago e Asaas** não passam pela aplicação: o app envia apenas o
  comprovante e o servidor valida pelos adaptadores de provedor, que leem os segredos do OpenBao.

## 15.4 — Observabilidade

- Dashboards de saúde versionados em `infra/observability/grafana/dashboards/homefinance-saude.json`
  e provisionados por `provisioning/dashboards/dashboards.yaml` (carga automática ao subir o
  Grafana). Painéis para **métricas** (req/s, latência p95, taxa de erro 5xx, heap .NET via
  Prometheus), **logs** (Loki) e **traces** (Tempo), com os datasources já provisionados.
- A **validação ponta a ponta** (métricas/logs/traces reais fluindo) depende da stack de
  observabilidade no ar. Neste ambiente, as imagens de `minio`/observabilidade não puxaram do
  Docker Hub, então a validação ao vivo fica pendente de um ambiente com as imagens disponíveis.

## 15.5 — Suíte de testes

Contra o PostgreSQL real (stack local, serviço `postgres`):

- Unitário: **494** aprovados.
- Integração: **55** aprovados.
- E2E: **1** aprovado, **6** `Skip` (jornadas Appium, pendentes de dispositivo).
- Build da solução: **0 erros / 0 avisos**.

## 15.6 — OpenSpec

`openspec validate "homefinance-nucleo-fase-1"` → **Change is valid**. Sem pendências para o
arquivamento.
