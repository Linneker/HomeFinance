import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// O portal fala com a Api por caminho relativo (/api). Em dev, o proxy encaminha para a Api
// local, então não há CORS a configurar; em produção, o gateway serve o portal e a Api sob a
// mesma origem. O alvo do proxy é sobrescrevível por VITE_API_ALVO.
const alvoApi = process.env.VITE_API_ALVO ?? "http://localhost:5286";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: alvoApi,
        changeOrigin: true,
      },
    },
  },
});
