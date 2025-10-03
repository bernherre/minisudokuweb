/// <reference types="vitest" />
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";

export default defineConfig({
    plugins: [
        react(),
        VitePWA({
            registerType: "autoUpdate",
            injectRegister: "auto",
            manifest: {
                name: "Sudoku Boxed",
                short_name: "Sudoku",
                description: "Sudoku 4x4 y 6x6 — PWA offline.",
                theme_color: "#0f172a",
                background_color: "#0f172a",
                display: "standalone",
                start_url: "./",
                scope: "./",
                icons: [
                    { src: "/icons/icon-192.png", sizes: "192x192", type: "image/png" },
                    { src: "/icons/icon-512.png", sizes: "512x512", type: "image/png" },
                    { src: "/icons/maskable-192.png", sizes: "192x192", type: "image/png", purpose: "maskable" },
                    { src: "/icons/maskable-512.png", sizes: "512x512", type: "image/png", purpose: "maskable" }
                ]
            },
            workbox: {
                navigateFallback: "/index.html",
                globPatterns: ["**/*.{js,css,html,png,svg,ico}"]
            }
        })
    ],
    base: "./",
    test: {
        globals: true,
        environment: "jsdom",
        setupFiles: "./src/setupTests.ts"
    }
});
