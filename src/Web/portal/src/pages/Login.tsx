import { useState, type FormEvent } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { useAutenticacao } from "../auth";
import { ErroApi } from "../api";

export function Login() {
  const { autenticado, entrar } = useAutenticacao();
  const navegar = useNavigate();
  const local = useLocation() as { state?: { de?: string } };
  const [email, setEmail] = useState("");
  const [senha, setSenha] = useState("");
  const [erro, setErro] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  if (autenticado) return <Navigate to={local.state?.de ?? "/contas"} replace />;

  async function enviar(evento: FormEvent) {
    evento.preventDefault();
    setErro(null);
    setEnviando(true);
    try {
      await entrar(email, senha);
      navegar(local.state?.de ?? "/contas", { replace: true });
    } catch (e) {
      setErro(e instanceof ErroApi ? e.message : "Não foi possível entrar.");
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="centro-tela">
      <form className="cartao login" onSubmit={enviar}>
        <h1>Back-office</h1>
        <p className="suave" style={{ marginTop: 0 }}>
          Acesso da operação do HomeFinance.
        </p>

        {erro && (
          <div className="alerta erro" role="alert" style={{ marginBottom: "1rem" }}>
            {erro}
          </div>
        )}

        <div className="campo">
          <label htmlFor="email">E-mail</label>
          <input
            id="email"
            type="email"
            autoComplete="username"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </div>

        <div className="campo">
          <label htmlFor="senha">Senha</label>
          <input
            id="senha"
            type="password"
            autoComplete="current-password"
            value={senha}
            onChange={(e) => setSenha(e.target.value)}
            required
          />
        </div>

        <button type="submit" disabled={enviando} style={{ width: "100%" }}>
          {enviando ? "Entrando…" : "Entrar"}
        </button>
      </form>
    </div>
  );
}
