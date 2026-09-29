import { useCallback, useEffect, useState, type FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { api, ErroApi, type EntradaHistorico } from "../api";
import { formatarDataHora, rotuloEvento } from "../formato";

export function Exportacao() {
  const { usuarioId = "" } = useParams();
  const [historico, setHistorico] = useState<EntradaHistorico[] | null>(null);
  const [motivo, setMotivo] = useState("");
  const [erro, setErro] = useState<string | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  const carregar = useCallback(async () => {
    try {
      setHistorico(await api.historicoExportacao(usuarioId));
    } catch (e) {
      setErro(e instanceof ErroApi ? e.message : "Falha ao carregar o histórico.");
    }
  }, [usuarioId]);

  useEffect(() => {
    void carregar();
  }, [carregar]);

  async function agir(acao: "habilitar" | "desabilitar", evento: FormEvent) {
    evento.preventDefault();
    setErro(null);
    setAviso(null);
    if (!motivo.trim()) {
      setErro("Informe o motivo — ele fica registrado na trilha de auditoria.");
      return;
    }
    setEnviando(true);
    try {
      if (acao === "habilitar") await api.habilitarExportacao(usuarioId, motivo.trim());
      else await api.desabilitarExportacao(usuarioId, motivo.trim());
      setAviso(acao === "habilitar" ? "Exportação habilitada." : "Exportação desabilitada.");
      setMotivo("");
      await carregar();
    } catch (e) {
      setErro(e instanceof ErroApi ? e.message : "Não foi possível concluir a ação.");
    } finally {
      setEnviando(false);
    }
  }

  const ultimoEvento = historico?.[0];
  const habilitadaAgora = ultimoEvento?.evento === 1;

  return (
    <div className="pilha">
      <div>
        <Link to="/contas" className="suave">
          ← Contas
        </Link>
        <h1 style={{ margin: "0.5rem 0 0" }}>Exportação de dados</h1>
        <p className="suave" style={{ margin: 0 }}>
          Conta <code>{usuarioId}</code>
        </p>
      </div>

      {historico !== null && (
        <div className="cartao linha" style={{ justifyContent: "space-between" }}>
          <span>
            Situação atual:{" "}
            <span className={`etiqueta ${habilitadaAgora ? "ativa" : ""}`}>
              {historico.length === 0 ? "Nunca habilitada" : habilitadaAgora ? "Habilitada" : "Desabilitada"}
            </span>
          </span>
        </div>
      )}

      <form className="cartao pilha">
        <h2 style={{ margin: 0 }}>Habilitar ou desabilitar</h2>
        <p className="suave" style={{ margin: 0 }}>
          A exportação abre acesso ao dado financeiro do titular. O motivo é obrigatório e fica na trilha de
          auditoria.
        </p>

        {erro && (
          <div className="alerta erro" role="alert">
            {erro}
          </div>
        )}
        {aviso && (
          <div className="alerta sucesso" role="status">
            {aviso}
          </div>
        )}

        <div className="campo" style={{ marginBottom: 0 }}>
          <label htmlFor="motivo">Motivo</label>
          <textarea
            id="motivo"
            rows={3}
            value={motivo}
            onChange={(e) => setMotivo(e.target.value)}
            placeholder="Ex.: requisição de acesso do titular por e-mail em 28/09."
            required
          />
        </div>

        <div className="linha">
          <button type="submit" disabled={enviando} onClick={(e) => agir("habilitar", e)}>
            {enviando ? "Enviando…" : "Habilitar"}
          </button>
          <button type="submit" className="perigo" disabled={enviando} onClick={(e) => agir("desabilitar", e)}>
            Desabilitar
          </button>
        </div>
      </form>

      <div className="cartao" style={{ padding: 0, overflowX: "auto" }}>
        <h2 style={{ margin: 0, padding: "1rem 1rem 0" }}>Histórico</h2>
        {historico === null ? (
          <p className="vazio">Carregando…</p>
        ) : historico.length > 0 ? (
          <table>
            <thead>
              <tr>
                <th>Quando</th>
                <th>Ação</th>
                <th>Motivo</th>
                <th>Autor</th>
              </tr>
            </thead>
            <tbody>
              {historico.map((entrada) => (
                <tr key={entrada.id}>
                  <td>{formatarDataHora(entrada.quando)}</td>
                  <td>
                    <span className={`etiqueta ${entrada.evento === 1 ? "ativa" : ""}`}>
                      {rotuloEvento(entrada.evento)}
                    </span>
                  </td>
                  <td>{entrada.motivo ?? <span className="suave">—</span>}</td>
                  <td className="suave">
                    <code>{entrada.autorId.slice(0, 8)}</code>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <p className="vazio">Nenhuma habilitação registrada para esta conta.</p>
        )}
      </div>
    </div>
  );
}
