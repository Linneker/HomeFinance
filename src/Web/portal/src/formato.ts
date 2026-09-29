import { EventoAuditoria, SituacaoAssinatura } from "./api";

const dataHora = new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" });
const data = new Intl.DateTimeFormat("pt-BR", { dateStyle: "short" });
const moeda = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });

export const formatarDataHora = (iso: string) => dataHora.format(new Date(iso));
export const formatarData = (iso: string) => data.format(new Date(iso));
export const formatarMoeda = (valor: number) => moeda.format(valor);

export function rotuloSituacao(situacao: SituacaoAssinatura | null): { texto: string; classe: string } {
  switch (situacao) {
    case SituacaoAssinatura.Ativa:
      return { texto: "Ativa", classe: "ativa" };
    case SituacaoAssinatura.Carencia:
      return { texto: "Carência", classe: "carencia" };
    case SituacaoAssinatura.Suspensa:
      return { texto: "Suspensa", classe: "suspensa" };
    case SituacaoAssinatura.Cancelada:
      return { texto: "Cancelada", classe: "cancelada" };
    default:
      return { texto: "Sem assinatura", classe: "" };
  }
}

export const rotuloEvento = (evento: EventoAuditoria) =>
  evento === EventoAuditoria.ExportacaoHabilitada ? "Habilitada" : "Desabilitada";
