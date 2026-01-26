import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      "/worker": {
        target: "http://localhost:5263",
        changeOrigin: true,
      },
      "/orders": {
        target: "http://localhost:5263",
        changeOrigin: true,
      },
      "/outbox": {
        target: "http://localhost:5263",
        changeOrigin: true,
      },
    },
  },
});
