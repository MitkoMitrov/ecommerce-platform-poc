import '@testing-library/jest-dom/vitest'
import { afterAll, afterEach, beforeAll } from 'vitest'
import { cleanup } from '@testing-library/react'
import { resetMockData } from './handlers'
import { server } from './server'

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' })
})

afterEach(() => {
  cleanup()
  server.resetHandlers()
  resetMockData()
  window.localStorage.clear()
})

afterAll(() => {
  server.close()
})
