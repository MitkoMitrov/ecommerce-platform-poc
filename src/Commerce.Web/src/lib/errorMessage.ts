import { ApiError } from '../api/apiClient'

export interface ErrorDescription {
  title: string
  detail?: string
  traceId?: string
}

interface ApiErrorShape {
  status: number
  title: string
  detail?: string
  traceId?: string
}

function isApiErrorShape(error: unknown): error is ApiErrorShape {
  return (
    typeof error === 'object' &&
    error !== null &&
    'status' in error &&
    'title' in error &&
    typeof (error as ApiErrorShape).status === 'number' &&
    typeof (error as ApiErrorShape).title === 'string'
  )
}

export function describeError(error: unknown, fallbackTitle: string): ErrorDescription {
  if (error instanceof ApiError || isApiErrorShape(error)) {
    return {
      title: error.title || fallbackTitle,
      detail: error.detail,
      traceId: error.traceId,
    }
  }

  return {
    title: fallbackTitle,
    detail: 'Please try again.',
  }
}
