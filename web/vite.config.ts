import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'VITE_')
  const apiTarget = env.VITE_API_PROXY_TARGET || 'http://localhost:5080'

  return {
    plugins: [
      react(),
      tailwindcss(),
      VitePWA({
        registerType: 'autoUpdate',
        includeAssets: ['favicon.svg'],
        manifest: {
          name: 'LeezenPass',
          short_name: 'LeezenPass',
          description: 'Digitaler Fahrradpass für Münster',
          lang: 'de',
          start_url: '/',
          display: 'standalone',
          theme_color: '#0f766e',
          background_color: '#ffffff',
          icons: [{ src: 'pwa-icon.svg', sizes: 'any', type: 'image/svg+xml', purpose: 'any' }],
        },
        workbox: {
          // API responses are never served from the service worker cache.
          navigateFallbackDenylist: [/^\/api\//],
        },
      }),
    ],
    server: {
      // Same-origin in dev so the auth cookie just works.
      proxy: {
        '/api': { target: apiTarget, changeOrigin: true },
      },
    },
  }
})
