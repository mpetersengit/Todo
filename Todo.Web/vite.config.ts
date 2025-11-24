import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    open: true, // Automatically open browser
    port: 5173, // Explicit port
    proxy: {
      '/todos': {
        target: 'http://localhost:5245',
        changeOrigin: true,
      },
    },
  },
})
