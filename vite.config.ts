// vite.config.ts
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
    plugins: [react()],
    // El Action setea VITE_BASE=/<repo>/ en build. Localmente sigue siendo "/".
    base: process.env.VITE_BASE || "/",
});
