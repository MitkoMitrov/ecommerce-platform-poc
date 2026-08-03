import { useGetCartSessionQuery } from '../../api/commerceApiSlice'

// Thin, component-facing wrapper around the generated RTK Query hook. The actual session
// algorithm (localStorage handling, stale-Cart recovery, StrictMode-safe creation) lives in
// commerceApiSlice.ts alongside the getCartSession endpoint it backs — this file exists only so
// callers keep a stable, descriptive import name.
export function useCartSession() {
  return useGetCartSessionQuery()
}
