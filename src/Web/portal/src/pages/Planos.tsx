import { useEffect, useState, type FormEvent } from "react";
import { api, ErroApi, type Plano, type PlanoEntrada } from "../api";
import { formatarMoeda } from "../formato";

const vazio: PlanoEntrada = { codigo: "", nome: "", cotaCompartilhamentos: 0, precoMensal: 0, ativo: true };

export function Planos() {
  const [planos, setPlanos] = useState<Plano[] | null>(null);
  const [form, setForm] = useState<PlanoEntrada>(vazio);
  const [editandoExistente, setEditandoExistente] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const [salvando, setSalvando] = useState(false);

  async function carregar() {
    try {
      setPlanos(await api.listarPlanos());
    } catch (e) {
      setErro(e instanceof ErroApi ? e.message : "Falha ao carregar planos.");
    }
  }

  useEffect(() => {
    void carregar();
  }, []);

  function editar(plano: Plano) {
    setForm({
      codigo: plano.codigo,
      nome: plano.nome,
      cotaCompartilhamentos: plano.cotaCompartilhamentos,
      precoMensal: plano.precoMensal,
      ativo: plano.ativo,
    });
    setEditandoExistente(true);
    setErro(null);
    setAviso(null);
  }

  function novo() {
    setForm(vazio);
    setEditandoExistente(false);
    setErro(null);
    setAviso(null);
  }

  async function salvar(evento: FormEvent) {
    evento.preventDefault();
    setErro(null);
    setAviso(null);
    setSalvando(true);
    try {
      const resultado = await api.salvarPlano(form);
      setAviso(resultado.criado ? "Plano criado." : "Plano atualizado.");
      await carregar();
      if (!editandoExistente) novo();
    } catch (e) {
      setErro(e instanceof ErroApi ? e.message : "Falha ao salvar o plano.");
    } finally {
      setSalvando(false);
    }
  }

  return (
    <div className="pilha">
      <h1 style={{ margin: 0 }}>Catálogo de planos</h1>

      <div style={{ display: "grid", gap: "1.5rem", gridTemplateColumns: "minmax(0, 1fr)" }}>
        <div className="cartao" style={{ padding: 0, overflowX: "auto" }}>
          {planos === null && !erro ? (
            <p className="vazio">Carregando…</p>
          ) : planos && planos.length > 0 ? (
            <table>
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Nome</th>
                  <th>Cota compart.</th>
                  <th>Preço</th>
                  <th>Situação</th>
                  <th aria-label="Ações" />
                </tr>
              </thead>
              <tbody>
                {planos.map((plano) => (
                  <tr key={plano.id}>
                    <td>{plano.codigo}</td>
                    <td>{plano.nome}</td>
                    <td>{plano.cotaCompartilhamentos}</td>
                    <td>{formatarMoeda(plano.precoMensal)}</td>
                    <td>
                      <span className={`etiqueta ${plano.ativo ? "ativa" : "cancelada"}`}>
                        {plano.ativo ? "Ativo" : "Inativo"}
                      </span>
                    </td>
                    <td style={{ textAlign: "right" }}>
                      <button className="secundario" onClick={() => editar(plano)}>
                        Editar
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <p className="vazio">Nenhum plano no catálogo.</p>
          )}
        </div>

        <form className="cartao pilha" onSubmit={salvar}>
          <div className="linha" style={{ justifyContent: "space-between" }}>
            <h2 style={{ margin: 0 }}>{editandoExistente ? "Editar plano" : "Novo plano"}</h2>
            {editandoExistente && (
              <button type="button" className="secundario" onClick={novo}>
                Novo plano
              </button>
            )}
          </div>

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

          <div className="campo">
            <label htmlFor="codigo">Código</label>
            <input
              id="codigo"
              value={form.codigo}
              onChange={(e) => setForm({ ...form, codigo: e.target.value })}
              disabled={editandoExistente}
              required
            />
            {editandoExistente && <small className="suave">O código identifica o plano e não muda.</small>}
          </div>

          <div className="campo">
            <label htmlFor="nome">Nome</label>
            <input id="nome" value={form.nome} onChange={(e) => setForm({ ...form, nome: e.target.value })} required />
          </div>

          <div className="linha" style={{ alignItems: "flex-start" }}>
            <div className="campo" style={{ flex: 1, minWidth: 140 }}>
              <label htmlFor="cota">Cota de compartilhamentos</label>
              <input
                id="cota"
                type="number"
                min={0}
                value={form.cotaCompartilhamentos}
                onChange={(e) => setForm({ ...form, cotaCompartilhamentos: Number(e.target.value) })}
                required
              />
            </div>
            <div className="campo" style={{ flex: 1, minWidth: 140 }}>
              <label htmlFor="preco">Preço mensal (R$)</label>
              <input
                id="preco"
                type="number"
                min={0}
                step="0.01"
                value={form.precoMensal}
                onChange={(e) => setForm({ ...form, precoMensal: Number(e.target.value) })}
                required
              />
            </div>
          </div>

          <label className="linha" style={{ fontWeight: 400 }}>
            <input
              type="checkbox"
              checked={form.ativo}
              onChange={(e) => setForm({ ...form, ativo: e.target.checked })}
              style={{ width: "auto" }}
            />
            Plano ativo (disponível para contratação)
          </label>

          <div>
            <button type="submit" disabled={salvando}>
              {salvando ? "Salvando…" : editandoExistente ? "Salvar alterações" : "Criar plano"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
