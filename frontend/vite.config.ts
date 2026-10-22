/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  // `.trycloudflare.com`: permite abrir el servidor de desarrollo por un túnel de Cloudflare, que el checkout de Wompi exige para la dirección de retorno (no acepta localhost).
  server: { port: 5173, host: true, allowedHosts: ['.trycloudflare.com'] },
  test: {
    environment: 'jsdom',
    include: ['pruebas/**/*.test.ts', 'pruebas/**/*.test.tsx'],
  },
});
