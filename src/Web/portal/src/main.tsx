import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { ProvedorDeAutenticacao } from "./auth";
import { App } from "./App";
import "./index.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <BrowserRouter>
      <ProvedorDeAutenticacao>
        <App />
      </ProvedorDeAutenticacao>
    </BrowserRouter>
  </StrictMode>,
);
