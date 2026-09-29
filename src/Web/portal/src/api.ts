// Camada de acesso à Api administrativa. Tudo relativo a /api/v1/admin/* — o proxy do Vite
// (dev) ou o gateway (produção) resolve a origem, então não há URL absoluta aqui.

const CHAVE_TOKEN = "homefinance.admin.token";

export function lerToken(): string | null {
  try {
    return sessionStorage.getItem(CHAVE_TOKEN);
  } catch {
    return null;
  }
}

export function guardarToken(token: string): void {
  try {
    sessionStorage.setItem(CHAVE_TOKEN, token);
  } catch {
    /* modo privado ou storage bloqueado: a sessão vive só em memória nesta aba */
  }
}

export function limparToken(): void {
  try {
    sessionStorage.removeItem(CHAVE_TOKEN);
  } catch {
    /* nada a fazer */
  }
}

/// <summary>Erro de API com o código de domínio, quando a resposta o traz.</summary>
export class ErroApi extends Error {
  constructor(
    public readonly status: number,
    public readonly codigo: string | null,
    mensagem: string,
  ) {
    super(mensagem);
    this.name = "ErroApi";
  }
}

interface ErroCorpo {
  erros?: { codigo?: string; mensagem?: string; campo?: string | null }[];
}

async function requisitar<T>(caminho: string, opcoes: RequestInit = {}): Promise<T> {
  const token = lerToken();
  const cabecalhos = new Headers(opcoes.headers);
  cabecalhos.set("Accept", "application/json");
  if (opcoes.body) cabecalhos.set("Content-Type", "application/json");
  if (token) cabecalhos.set("Authorization", `Bearer ${token}`);

  const resposta = await fetch(`/api/v1/${caminho}`, { ...opcoes, headers: cabecalhos });

  // Sessão expirada ou credencial recusada: derruba o token para a UI voltar ao login.
  if (resposta.status === 401) {
    limparToken();
    throw new ErroApi(401, null, "Sessão expirada. Entre novamente.");
  }

  if (!resposta.ok) {
    let codigo: string | null = null;
    let mensagem = `Falha na requisição (${resposta.status}).`;
    try {
      const corpo = (await resposta.json()) as ErroCorpo;
      const primeiro = corpo.erros?.[0];
      if (primeiro) {
        codigo = primeiro.codigo ?? null;
        mensagem = primeiro.mensagem ?? mensagem;
      }
    } catch {
      /* resposta sem corpo JSON */
    }
    throw new ErroApi(resposta.status, codigo, mensagem);
  }

  if (resposta.status === 204) return undefined as T;
  return (await resposta.json()) as T;
}

// ---------------------------------------------------------------- tipos

export enum SituacaoAssinatura {
  Ativa = 1,
  Carencia = 2,
  Suspensa = 3,
  Cancelada = 4,
}

export enum EventoAuditoria {
  ExportacaoHabilitada = 1,
  ExportacaoDesabilitada = 2,
}

export interface Sessao {
  token: string;
  expiraEm: string;
}

export interface Conta {
  usuarioId: string;
  email: string;
  criadoEm: string;
  situacaoAssinatura: SituacaoAssinatura | null;
  planoCodigo: string | null;
  planoNome: string | null;
}

export interface Plano {
  id: string;
  codigo: string;
  nome: string;
  cotaCompartilhamentos: number;
  precoMensal: number;
  ativo: boolean;
}

export interface PlanoEntrada {
  codigo: string;
  nome: string;
  cotaCompartilhamentos: number;
  precoMensal: number;
  ativo: boolean;
}

export interface EntradaHistorico {
  id: string;
  evento: EventoAuditoria;
  autorId: string;
  motivo: string | null;
  quando: string;
}

// ---------------------------------------------------------------- chamadas

export const api = {
  autenticar: (email: string, senha: string) =>
    requisitar<Sessao>("admin/sessoes", {
      method: "POST",
      body: JSON.stringify({ email, senha }),
    }),

  listarContas: () => requisitar<Conta[]>("admin/contas"),

  listarPlanos: () => requisitar<Plano[]>("admin/planos"),

  salvarPlano: (plano: PlanoEntrada) =>
    requisitar<{ planoId: string; criado: boolean }>("admin/planos", {
      method: "POST",
      body: JSON.stringify(plano),
    }),

  habilitarExportacao: (contaUsuarioId: string, motivo: string) =>
    requisitar<void>(`admin/contas/${contaUsuarioId}/exportacao/habilitacao`, {
      method: "POST",
      body: JSON.stringify({ motivo }),
    }),

  desabilitarExportacao: (contaUsuarioId: string, motivo: string) =>
    requisitar<void>(`admin/contas/${contaUsuarioId}/exportacao/desabilitacao`, {
      method: "POST",
      body: JSON.stringify({ motivo }),
    }),

  historicoExportacao: (contaUsuarioId: string) =>
    requisitar<EntradaHistorico[]>(`admin/contas/${contaUsuarioId}/exportacao/historico`),
};
