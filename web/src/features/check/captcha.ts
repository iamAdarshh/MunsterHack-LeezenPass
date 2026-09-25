/** Empty in dev (API uses the fake captcha); set VITE_TURNSTILE_SITE_KEY for real Turnstile. */
export const turnstileSiteKey = import.meta.env.VITE_TURNSTILE_SITE_KEY ?? ''

interface TurnstileApi {
  render(
    element: HTMLElement,
    options: {
      sitekey: string
      callback: (token: string) => void
      'expired-callback'?: () => void
      'error-callback'?: () => void
      language?: string
    },
  ): string
  reset(widgetId?: string): void
  remove(widgetId: string): void
}

declare global {
  interface Window {
    turnstile?: TurnstileApi
  }
}

let loading: Promise<TurnstileApi> | null = null

/** Loads the Turnstile script once (needs network). */
export function loadTurnstile(): Promise<TurnstileApi> {
  loading ??= new Promise((resolve, reject) => {
    const script = document.createElement('script')
    script.src = 'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit'
    script.async = true
    script.onload = () => (window.turnstile ? resolve(window.turnstile) : reject(new Error('turnstile missing')))
    script.onerror = () => {
      loading = null
      reject(new Error('turnstile failed to load'))
    }
    document.head.appendChild(script)
  })
  return loading
}
