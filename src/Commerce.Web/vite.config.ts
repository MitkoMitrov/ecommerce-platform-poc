/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const apiProxyTarget = 'http://127.0.0.1:5003'

const proxy = {
  '/api': {
    target: apiProxyTarget,
    changeOrigin: true,
  },
  '/health': {
    target: apiProxyTarget,
    changeOrigin: true,
  },
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    host: '127.0.0.1',
    port: 5173,
    strictPort: true,
    proxy,
  },
  preview: {
    host: '127.0.0.1',
    port: 5173,
    strictPort: true,
    proxy,
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    globals: false,
    css: true,
  },
})
