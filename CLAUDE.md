# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Estado atual do repositório

A solução .NET existe e compila. A implementação segue a change `homefinance-nucleo-fase-1` — os
grupos 1 a 6 estão concluídos (66/176); o restante está pendente. Consulte `tasks.md` da change
antes de assumir que algo existe.

Repositório git com remoto em `github.com/Linneker/HomeFinance`, branch `main`. O histórico
é dividido por grupo da change (um commit por grupo). **Commit publicado não se desfaz**: um
segredo que entre no repositório está exposto mesmo que um commit posterior o remova.

### Comandos

```bash
# stack local (PostgreSQL+pgvector, OpenBao, MinIO, RabbitMQ, OTel/Prometheus/Loki/Tempo/Grafana)
cd infra/compose && cp .env.example .env && docker compose up -d

dotnet build HomeFinance.slnx
dotnet run --project src/Api/Acme.Sistemas.HomeFinance.Api     # liveness em /health/live

dotnet test                                                     # tudo
dotnet test test/Unit/Acme.Sistemas.Services.UnitTest           # um projeto
dotnet test --filter "FullyQualifiedName~MediatorTests"         # uma classe
dotnet test --filter "FullyQualifiedName~MediatorTests.SendAsync_deve_resolver_handler_e_devolver_resultado"

python ~/.claude/skills/acme-blueprint/scripts/validar_estrutura.py .   # conformidade
```

Detalhes de setup (segredos no OpenBao, portas, estrutura de pastas): `README.md`.

### Armadilhas conhecidas do ambiente

- **`dotnet add package` falha com 401.** Há um feed NuGet privado (Azure DevOps `Motz-BR`)
  configurado na máquina que exige autenticação e quebra a restauração de pacotes novos. Contorne
  com `--source https://api.nuget.org/v3/index.json`.
- **Segredo nunca vai para `appsettings`.** Connection strings vêm do OpenBao e o token do cofre vem
  da variável `OPENBAO_TOKEN`. A Api **sobe** sem eles e `/health/live` responde — a falha só
  aparece na primeira leitura de segredo. É intencional: liveness não pode depender de
  dependência externa, senão o Kubernetes reinicia a aplicação sempre que o cofre oscilar. Quem
  cobre isso é o readiness, que entra junto com as migrations no grupo 2.

## Fluxo de trabalho: OpenSpec (spec-driven)

Toda mudança passa pelo OpenSpec antes da implementação. `openspec/config.yaml` define
`schema: spec-driven`, cujos artefatos são, em ordem de dependência:

1. `proposal.md` — o quê e por quê
2. `specs/<capability>/spec.md` — delta da especificação (não a spec principal)
3. `design.md` — como (condicional; pode ser pulado)
4. `tasks.md` — passos de implementação

Comandos (skills instaladas neste repo):

| Comando | Uso |
|---|---|
| `/opsx:explore` | Pensar sobre o problema antes de propor |
| `/opsx:propose` | Cria a change e gera todos os artefatos |
| `/opsx:apply` | Implementa as tasks, marcando `- [ ]` → `- [x]` |
| `/opsx:archive` | Move a change concluída para `openspec/changes/archive/` |

CLI por baixo das skills (v1.7.0, disponível no PATH):

```bash
openspec new change "<nome-kebab-case>"
openspec status --change "<nome>" --json      # ordem dos artefatos, applyRequires, paths
openspec instructions <artifact-id> --change "<nome>" --json
openspec list --json
openspec validate --change "<nome>"
openspec archive "<nome>"
```

Regras que mais causam erro na prática:

- `status` só verifica existência de arquivo. Um artefato marcado `done` **não** garante que suas
  dependências existem — use as arestas `requires`, não o `status`, para montar o conjunto exigido.
- `context` e `rules` retornados por `openspec instructions` são restrições para você; nunca copie
  esses blocos para dentro do artefato gerado.
- Releia os artefatos de dependência do disco antes de escrever um novo (o usuário pode tê-los
  editado fora da conversa).

`openspec/config.yaml` está com todas as seções opcionais comentadas. É ali que devem entrar o
contexto do projeto (stack, domínio) e as regras por artefato quando forem definidos — assim eles
passam a ser injetados automaticamente na geração dos artefatos.

## Arquitetura: blueprint Acme

Este projeto fica sob `H:\ACME\PROJETOS\Sistemas\`, junto de outros sistemas Acme. A skill global
**`acme-blueprint`** (blueprint 2.1.0) define o padrão arquitetural obrigatório da plataforma e deve
ser consultada **antes** de criar a solução, adicionar Command/Query/Event, repositório, entidade,
migration ou decidir em que camada um arquivo vive.

Pontos da stack que já ficam definidos por ela (não escolha alternativas sem apontar o conflito ao
usuário): .NET em camadas com CQRS/Mediator **próprio** (não MediatR), PostgreSQL com **SQL puro**
via `IDataConfiguration` (sem ORM), pgvector, OpenBao para segredos, MinIO, RabbitMQ, OpenTelemetry,
Docker/Kubernetes, MAUI, portal React, xUnit + Moq + Bogus + AutoMock.

## Ambiente

Windows 11 + PowerShell 5.1. As tools Bash (Git Bash) e PowerShell coexistem — cada uma exige sua
própria sintaxe; em PowerShell 5.1 não existem `&&`/`||`.
