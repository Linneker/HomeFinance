import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
import { api, guardarToken, limparToken, lerToken } from "./api";

interface Autenticacao {
  autenticado: boolean;
  entrar: (email: string, senha: string) => Promise<void>;
  sair: () => void;
}

const Contexto = createContext<Autenticacao | null>(null);

export function ProvedorDeAutenticacao({ children }: { children: ReactNode }) {
  const [autenticado, setAutenticado] = useState<boolean>(() => lerToken() !== null);

  const valor = useMemo<Autenticacao>(
    () => ({
      autenticado,
      entrar: async (email, senha) => {
        const sessao = await api.autenticar(email, senha);
        guardarToken(sessao.token);
        setAutenticado(true);
      },
      sair: () => {
        limparToken();
        setAutenticado(false);
      },
    }),
    [autenticado],
  );

  return <Contexto.Provider value={valor}>{children}</Contexto.Provider>;
}

export function useAutenticacao(): Autenticacao {
  const contexto = useContext(Contexto);
  if (!contexto) throw new Error("useAutenticacao fora do ProvedorDeAutenticacao.");
  return contexto;
}
