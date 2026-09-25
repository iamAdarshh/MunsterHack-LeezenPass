/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Cloudflare Turnstile site key. Empty = no captcha widget (API runs with fakes). */
  readonly VITE_TURNSTILE_SITE_KEY?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
