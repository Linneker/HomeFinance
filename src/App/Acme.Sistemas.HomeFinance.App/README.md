# HomeFinance — App (MAUI)

Superfície do usuário final. .NET MAUI, MVVM leve (páginas com injeção de dependência), falando
com a Api por HTTP (`/api/v1/*`) como o portal — não referencia as camadas de servidor.

## Build e alvos

Nesta máquina só o workload **`maui-windows`** está instalado, então o build fica no alvo
Windows:

```bash
dotnet build src/App/Acme.Sistemas.HomeFinance.App
```

Os alvos móveis entram quando os workloads existirem:

```bash
dotnet build src/App/Acme.Sistemas.HomeFinance.App -p:IncluirMovel=true
# requer: dotnet workload install maui-android maui-ios maui-maccatalyst
```

O código de UI e de serviços é MAUI puro; o específico de plataforma fica nas costuras abaixo e em
`Platforms/`.

## Endereço da Api

`Services/ApiConfig.cs` aponta para a Api local (`http://localhost:5286`; no Android o emulador usa
`http://10.0.2.2:5286`). Suba a Api e a stack (`infra/compose`) antes de rodar o app.

## Arquitetura

- `Services/ApiClient.cs` — HTTP tipado: injeta o token e o cabeçalho `X-Tenant`, trata 401
  (derruba a sessão) e distingue o 409 de duplicidade de lançamento.
- `Services/Sessao.cs` — token no `SecureStorage` da plataforma; expiração encerra a sessão.
- `Services/ContextoTenant.cs` — tenant ativo e modo somente leitura (barra permanente na UI).
- `Services/IdentidadeApi.cs`, `Services/FinancasApi.cs` — chamadas por feature.
- `Pages/` — Login, CriarConta, Dashboard, Lançamentos, CriarLançamento, Orçamento, Renda,
  Alertas, Resumo, Regras/Categorias, Assinatura, Planos, Compartilhamento, Exportação, Tenants,
  Mais.
- `AppShell` — gate de login + `TabBar` da área autenticada; rotas secundárias registradas em código.

## Costuras dependentes de plataforma (abstraídas)

- `ICompraApp` (`Services/ICompraApp.cs`) — compra in-app / checkout do gateway. A implementação de
  loja substitui `CompraAppSimulada`. Nenhum dado de cartão passa pelo app; o app recebe só o
  comprovante e **o servidor valida antes de liberar o recurso**.
- `INotificacaoDispositivo` (`Services/INotificacaoDispositivo.cs`) — push de alertas ao dono do
  tenant. Stub sem entrega nesta fase; a central de alertas (pull) cobre o intervalo. A entrega
  real usa APNs/FCM/WNS e um endpoint de registro de dispositivo.

## Testes E2E (Appium)

As jornadas estão em `test/E2E/.../JornadasDoAppTests.cs` como `Skip`: Appium exige
emulador/dispositivo e servidor Appium, indisponíveis no CI de unidade headless. Ao habilitar
Appium, remova o `Skip` e dirija cada jornada pela árvore de automação.
