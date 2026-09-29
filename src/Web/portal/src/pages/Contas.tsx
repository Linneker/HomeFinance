import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api, ErroApi, type Conta } from "../api";
import { formatarData, rotuloSituacao } from "../formato";

export function Contas() {
  const [contas, setContas] = useState<Conta[] | null>(null);
  const [erro, setErro] = useState<string | null>(null);
  const [busca, setBusca] = useState("");

  useEffect(() => {
    let vivo = true;
    api
      .listarContas()
      .then((dados) => vivo && setContas(dados))
      .catch((e) => vivo && setErro(e instanceof ErroApi ? e.message : "Falha ao carregar contas."));
    return () => {
      vivo = false;
    };
  }, []);

  const filtradas = contas?.filter((c) => c.email.toLowerCase().includes(busca.trim().toLowerCase()));

  return (
    <div className="pilha">
      <div className="linha" style={{ justifyContent: "space-between" }}>
        <h1 style={{ margin: 0 }}>Contas</h1>
        <input
          type="search"
          placeholder="Filtrar por e-mail…"
          aria-label="Filtrar contas por e-mail"
          value={busca}
          onChange={(e) => setBusca(e.target.value)}
          style={{ maxWidth: 260 }}
        />
      </div>

      {erro && (
        <div className="alerta erro" role="alert">
          {erro}
        </div>
      )}

      <div className="cartao" style={{ padding: 0, overflowX: "auto" }}>
        {contas === null && !erro ? (
          <p className="vazio">Carregando…</p>
        ) : filtradas && filtradas.length > 0 ? (
          <table>
            <thead>
              <tr>
                <th>E-mail</th>
                <th>Criada em</th>
                <th>Assinatura</th>
                <th>Plano</th>
                <th aria-label="Ações" />
              </tr>
            </thead>
            <tbody>
              {filtradas.map((conta) => {
                const s = rotuloSituacao(conta.situacaoAssinatura);
                return (
                  <tr key={conta.usuarioId}>
                    <td>{conta.email}</td>
                    <td>{formatarData(conta.criadoEm)}</td>
                    <td>
                      <span className={`etiqueta ${s.classe}`}>{s.texto}</span>
                    </td>
                    <td>{conta.planoNome ?? <span className="suave">—</span>}</td>
                    <td style={{ textAlign: "right" }}>
                      <Link to={`/contas/${conta.usuarioId}/exportacao`}>Exportação</Link>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        ) : (
          <p className="vazio">Nenhuma conta encontrada.</p>
        )}
      </div>
    </div>
  );
}
