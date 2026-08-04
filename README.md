# HomeFinance

Gestão financeira pessoal e familiar. Produto SaaS multi-tenant, mercado brasileiro.

Arquitetura conforme o blueprint Acme Sistemas 2.1.0: .NET em camadas com CQRS/Mediator próprio,
PostgreSQL com SQL puro (sem ORM), OpenBao para segredos, MinIO, RabbitMQ, OpenTelemetry.

## Pré-requisitos

- .NET SDK 10.0+
- Docker
- Node.js 20+ (portal administrativo, a partir do grupo 14)

## Subir a stack local

```bash
cd infra/compose
cp .env.example .env        # ajuste as senhas antes de subir
docker compose up -d
docker compose ps           # confirmar que tudo está healthy
```

Sobe PostgreSQL+pgvector, OpenBao, MinIO, RabbitMQ, OTel Collector, Prometheus, Loki, Tempo e
Grafana. Portas e credenciais saem do `.env` — que **não** é versionado.

| Serviço | URL local |
|---|---|
| PostgreSQL | `localhost:57432` |
| OpenBao | http://localhost:57200 |
| MinIO (console) | http://localhost:57901 |
| RabbitMQ (management) | http://localhost:57673 |
| Grafana | http://localhost:57000 |
| Prometheus | http://localhost:57090 |
| OTLP (gRPC / HTTP) | `localhost:57317` / `localhost:57318` |

### Segredos

A aplicação não lê connection string de `appsettings` — regra 15 do blueprint. Com o OpenBao em
modo dev, grave os segredos que a Api espera:

```bash
export BAO_ADDR=http://localhost:57200
export BAO_TOKEN=dev-root-token          # o mesmo do .env

bao kv put secret/homefinance \
  ConnectionStrings__ReadConnection="Host=localhost;Port=57432;Database=homefinance;Username=homefinance_owner;Password=<sua-senha>" \
  ConnectionStrings__WriteConnection="Host=localhost;Port=57432;Database=homefinance;Username=homefinance_owner;Password=<sua-senha>"
```

O token do OpenBao chega à Api pela variável de ambiente `OPENBAO_TOKEN` — nunca por arquivo de
configuração:

```bash
export OPENBAO_TOKEN=dev-root-token
```

> **A aplicação não conecta como dono do banco.** O usuário do `POSTGRES_USER` é superusuário, e
> superusuário **ignora RLS mesmo com `FORCE`** — silenciosamente, sem erro. As connection
> strings acima devem usar o role `homefinance_app`, criado pela primeira migration
> (`NOSUPERUSER NOBYPASSRLS`). A senha dele fica fora das migrations, para não versionar
> credencial; defina-a uma vez após migrar:
>
> ```bash
> docker exec homefinance-postgres psql -U homefinance_owner -d homefinance \
>   -c "ALTER ROLE homefinance_app WITH PASSWORD '<senha>'"
> ```
>
> Migrar continua sendo feito com o dono; a aplicação em runtime, nunca.

## Build

```bash
dotnet build HomeFinance.slnx
```

> Se o `dotnet add package` falhar com 401, é um feed NuGet privado configurado na máquina que exige
> autenticação. Contorne apontando a origem: `dotnet add <projeto> package <pacote> --source https://api.nuget.org/v3/index.json`

## Testes

```bash
dotnet test                                                    # tudo
dotnet test test/Unit/Acme.Sistemas.Services.UnitTest          # só unitários
dotnet test test/Integration/Acme.Sistemas.IntegrationTest     # integração (exige compose no ar)
dotnet test test/E2E/Acme.Sistemas.E2ETest                     # E2E (Appium)
```

Rodar um único teste:

```bash
dotnet test --filter "FullyQualifiedName~MediatorTests.SendAsync_deve_resolver_handler_e_devolver_resultado"
dotnet test --filter "FullyQualifiedName~MediatorTests"        # a classe inteira
```

## Executar a Api

```bash
dotnet run --project src/Api/Acme.Sistemas.HomeFinance.Api
```

Liveness em `GET /health/live`. Em Development, OpenAPI em `/openapi/v1.json`.

## Conformidade com o blueprint

```bash
python ~/.claude/skills/acme-blueprint/scripts/validar_estrutura.py .
```

## Estrutura

```
infra/          compose, k8s e configuração de observabilidade
src/Api/        Acme.Sistemas.HomeFinance.Api        — Minimal API
src/Service/    Acme.Sistemas.Services               — CQRS por funcionalidade
                Acme.Sistemas.Core                   — Mediator próprio, contratos, response
                Acme.Sistemas.Domain                 — entidades, enums, interfaces
src/Data/       Acme.Sistemas.Infrastructure         — banco, segredos, cache, GED, mensageria
                Acme.Sistemas.Repository             — SQL puro, repositórios Read/Write
                Acme.Sistemas.ExternalIntegration    — proxy HTTP por reflexão
src/App/        Acme.Sistemas.HomeFinance.App        — MAUI (grupo 13)
src/Web/portal/ portal administrativo React          — (grupo 14)
test/           Unit, Integration, E2E
openspec/       propostas, specs, design e tasks
```

## Planejamento

O trabalho é conduzido pelo OpenSpec. A change em andamento é `homefinance-nucleo-fase-1`:

```bash
openspec status --change homefinance-nucleo-fase-1
```
