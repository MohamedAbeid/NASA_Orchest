import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      manifestFilename: 'manifest.json',
      includeManifestIcons: false,
      manifest: {
        name: 'Orchest — Health Sensor Station',
        short_name: 'Orchest',
        description: 'Offline health sensor station: gaze, voice, typing and mood indicators from the camera.',
        lang: 'en',
        dir: 'ltr',
        start_url: '/',
        scope: '/',
        display: 'standalone',
        orientation: 'portrait',
        theme_color: '#050b18',
        background_color: '#050b18',
        icons: [
          { src: 'icons/icon-192.png', sizes: '192x192', type: 'image/png' },
          { src: 'icons/icon-512.png', sizes: '512x512', type: 'image/png' },
          { src: 'icons/icon-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' }
        ]
      },
      workbox: {
        globPatterns: ['**/*.{js,css,html}'],
        globIgnores: ['**/mediapipe/**', '**/icons/**'],
        maximumFileSizeToCacheInBytes: 30 * 1024 * 1024,
        navigateFallback: 'index.html',
        cleanupOutdatedCaches: true,
        runtimeCaching: [
          {
            urlPattern: /\/models\/.*\.task$/,
            handler: 'CacheFirst',
            options: { cacheName: 'orchest-models', expiration: { maxEntries: 4, maxAgeSeconds: 60 * 60 * 24 * 60 } }
          },
          {
            urlPattern: /\/mediapipe\/.*\.(wasm|js)$/,
            handler: 'CacheFirst',
            options: { cacheName: 'orchest-wasm', expiration: { maxEntries: 8, maxAgeSeconds: 60 * 60 * 24 * 60 } }
          }
        ]
      },
      devOptions: { enabled: false }
    })
  ],
  server: {
    host: true,
    port: 5173,
    proxy: {
      '/api': { target: 'http://localhost:5054', changeOrigin: true },
      '/hub': { target: 'http://localhost:5054', ws: true, changeOrigin: true }
    }
  },
  build: { outDir: 'dist' }
})
