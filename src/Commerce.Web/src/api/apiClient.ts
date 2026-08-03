const configuredBaseUrl = import.meta.env.VITE_API_BASE_URL as string | undefined

function resolveBaseUrl(): string {
  if (!configuredBaseUrl) {
    return ''
  }

  return configuredBaseUrl.endsWith('/')
    ? configuredBaseUrl.slice(0, -1)
    : configuredBaseUrl
}

const baseUrl = resolveBaseUrl()

export class ApiError extends Error {
  readonly status: number
  readonly title: string
  readonly detail?: string
  readonly traceId?: string

  constructor(status: number, title: string, detail?: string, traceId?: string) {
    super(detail ?? title)
    this.name = 'ApiError'
    this.status = status
    this.title = title
    this.detail = detail
    this.traceId = traceId
  }
}

interface ProblemDetailsBody {
  title?: string
  detail?: string
  traceId?: string
  [key: string]: unknown
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  signal?: AbortSignal
}

async function request<TResponse>(path: string, options: RequestOptions = {}): Promise<TResponse> {
  const { method = 'GET', body, signal } = options

  const headers: Record<string, string> = {
    Accept: 'application/json',
  }

  const hasBody = body !== undefined
  if (hasBody) {
    headers['Content-Type'] = 'application/json'
  }

  let response: Response
  try {
    response = await fetch(`${baseUrl}${path}`, {
      method,
      headers,
      body: hasBody ? JSON.stringify(body) : undefined,
      signal,
    })
  } catch {
    throw new ApiError(0, 'Network error', 'Unable to reach the server. Check your connection and try again.')
  }

  if (response.status === 204) {
    return undefined as TResponse
  }

  const contentType = response.headers.get('content-type') ?? ''

  if (!response.ok) {
    if (contentType.includes('application/problem+json') || contentType.includes('application/json')) {
      try {
        const problem = (await response.json()) as ProblemDetailsBody
        throw new ApiError(
          response.status,
          problem.title ?? 'Request failed',
          problem.detail,
          problem.traceId,
        )
      } catch (error) {
        if (error instanceof ApiError) {
          throw error
        }
        throw new ApiError(response.status, 'Request failed', 'The server returned an unexpected response.')
      }
    }

    throw new ApiError(response.status, 'Request failed', 'The server returned an unexpected response.')
  }

  if (!contentType.includes('application/json')) {
    return undefined as TResponse
  }

  try {
    return (await response.json()) as TResponse
  } catch {
    throw new ApiError(response.status, 'Invalid response', 'The server returned a response that could not be read.')
  }
}

export const apiClient = {
  get<TResponse>(path: string, signal?: AbortSignal): Promise<TResponse> {
    return request<TResponse>(path, { method: 'GET', signal })
  },
  post<TResponse>(path: string, body?: unknown, signal?: AbortSignal): Promise<TResponse> {
    return request<TResponse>(path, { method: 'POST', body, signal })
  },
  put<TResponse>(path: string, body?: unknown, signal?: AbortSignal): Promise<TResponse> {
    return request<TResponse>(path, { method: 'PUT', body, signal })
  },
  delete<TResponse>(path: string, signal?: AbortSignal): Promise<TResponse> {
    return request<TResponse>(path, { method: 'DELETE', signal })
  },
}
