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
          // Map tiles: keep what was viewed (e.g. during rehearsal) so the map still shows offline.
          runtimeCaching: [
            {
              urlPattern: /^https:\/\/tile\.openstreetmap\.org\//,
              handler: 'CacheFirst',
              options: {
                cacheName: 'osm-tiles',
                expiration: { maxEntries: 500, maxAgeSeconds: 7 * 24 * 60 * 60 },
                cacheableResponse: { statuses: [0, 200] },
              },
            },
          ],
        },
      }),
    ],
    server: {
      // Same-origin in dev so the auth cookie just works.
      proxy: {
        // xfwd: pass the client IP on (X-Forwarded-For) for per-IP rate limits; the API trusts it from loopback only.
        '/api': { target: apiTarget, changeOrigin: true, xfwd: true },
      },
    },
  }
})
