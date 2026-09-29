import { NavLink, Navigate, Route, Routes, useLocation } from "react-router-dom";
import { useAutenticacao } from "./auth";
import { Login } from "./pages/Login";
import { Contas } from "./pages/Contas";
import { Planos } from "./pages/Planos";
import { Exportacao } from "./pages/Exportacao";
import type { ReactNode } from "react";

function Protegida({ children }: { children: ReactNode }) {
  const { autenticado } = useAutenticacao();
  const local = useLocation();
  return autenticado ? <>{children}</> : <Navigate to="/login" replace state={{ de: local.pathname }} />;
}

function Layout({ children }: { children: ReactNode }) {
  const { sair } = useAutenticacao();
  return (
    <div className="app">
      <header className="topo">
        <span className="marca">HomeFinance · Back-office</span>
        <nav aria-label="Seções">
          <NavLink to="/contas" className={({ isActive }) => (isActive ? "ativo" : "")}>
            Contas
          </NavLink>
          <NavLink to="/planos" className={({ isActive }) => (isActive ? "ativo" : "")}>
            Planos
          </NavLink>
        </nav>
        <button className="secundario" onClick={sair}>
          Sair
        </button>
      </header>
      <main className="conteudo">{children}</main>
    </div>
  );
}

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route
        path="/contas"
        element={
          <Protegida>
            <Layout>
              <Contas />
            </Layout>
          </Protegida>
        }
      />
      <Route
        path="/contas/:usuarioId/exportacao"
        element={
          <Protegida>
            <Layout>
              <Exportacao />
            </Layout>
          </Protegida>
        }
      />
      <Route
        path="/planos"
        element={
          <Protegida>
            <Layout>
              <Planos />
            </Layout>
          </Protegida>
        }
      />
      <Route path="*" element={<Navigate to="/contas" replace />} />
    </Routes>
  );
}
