# HomeFinance — Portal administrativo (back-office)

Portal React (Vite + TypeScript) da operação. Autentica com identidade administrativa
(`perfil=administrador`), lista contas sem qualquer dado financeiro, mantém o catálogo de planos
e habilita/desabilita a exportação de dados por conta, com motivo obrigatório e histórico.

Fica fora do prefixo `Acme.Sistemas.*` por ser o portal web (convenção do blueprint).

## Rodar em desenvolvimento

```bash
npm install
npm run dev        # http://localhost:5173
```

O portal fala com a Api por caminho relativo (`/api/v1/admin/*`). Em dev, o Vite faz proxy para a
Api local — por padrão `http://localhost:5286` (perfil `http` da Api). Se a Api estiver em outra
porta, copie `.env.example` para `.env` e ajuste `VITE_API_ALVO`.

Suba a Api antes (precisa de OpenBao + Postgres da stack de `infra/compose`):

```bash
cd ../../../infra/compose && docker compose up -d
cd ../../ && dotnet run --project src/Api/Acme.Sistemas.HomeFinance.Api
```

## Build de produção

```bash
npm run build      # tsc + vite build → dist/
npm run preview    # serve o dist localmente
```

Em produção o `dist/` é servido pelo gateway sob a mesma origem da Api, então não há CORS.

## Provisionar um administrador

O login exige um administrador cadastrado. A tabela `administrador` não aceita escrita direta do
role da aplicação: o único caminho é a função `SECURITY DEFINER` `provisionar_administrador`,
acionada por `IAdministradorRepository.ProvisionarAsync` (ação de operação, nunca exposta em
endpoint). O hash da senha é gerado por `Acme.Sistemas.Core.Security.Hash` — por isso o
provisionamento é feito por ferramenta interna/rotina de bootstrap, não por SQL manual.

O teste de integração `BackOfficeNoBancoTests` demonstra o provisionamento e o login ponta a
ponta contra o Postgres real.

## Estrutura

- `src/api.ts` — camada tipada de acesso à Api e gestão do token de sessão.
- `src/auth.tsx` — contexto de autenticação (login, logout, guarda de rota).
- `src/pages/` — Login, Contas, Planos, Exportação.
- `src/formato.ts` — formatação pt-BR (datas, moeda) e rótulos de situação/evento.
