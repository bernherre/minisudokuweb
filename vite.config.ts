import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
    plugins: [react()],
    base: './', // rutas relativas: perfectas para Pages
})
"@ | Set-Content -Path "vite.config.ts" -Encoding UTF8
