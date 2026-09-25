/** RFC 7807 problem details as returned by ASP.NET Core / FastEndpoints. */
export interface ProblemDetails {
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]> | { name: string; reason: string }[]
}

export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails | null

  constructor(status: number, problem: ProblemDetails | null) {
    super(problem?.title ?? `HTTP ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

type JsonBody = Record<string, unknown> | unknown[]

interface ApiRequestInit extends Omit<RequestInit, 'body'> {
  /** Serialised as JSON. For uploads pass a FormData via `formData`. */
  json?: JsonBody
  formData?: FormData
}

/**
 * Same-origin fetch (Vite proxies /api in dev). Sends the auth cookie, throws ApiError on non-2xx,
 * returns undefined for 204.
 */
export async function apiFetch<T>(path: string, { json, formData, headers, ...init }: ApiRequestInit = {}): Promise<T> {
  const finalHeaders = new Headers(headers)
  finalHeaders.set('Accept', 'application/json')

  let body: BodyInit | undefined
  if (json !== undefined) {
    finalHeaders.set('Content-Type', 'application/json')
    body = JSON.stringify(json)
  } else if (formData) {
    // CSRF guard required by the API for multipart endpoints.
    finalHeaders.set('X-LeezenPass', '1')
    body = formData
  }

  const response = await fetch(path, { ...init, headers: finalHeaders, body, credentials: 'include' })

  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response))
  }
  if (response.status === 204 || response.headers.get('Content-Length') === '0') {
    return undefined as T
  }
  return (await response.json()) as T
}

async function readProblem(response: Response): Promise<ProblemDetails | null> {
  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return null
  }
}
