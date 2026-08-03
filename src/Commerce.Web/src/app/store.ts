import { configureStore } from '@reduxjs/toolkit'
import { commerceApi } from '../api/commerceApiSlice'

export function setupStore() {
  return configureStore({
    reducer: {
      [commerceApi.reducerPath]: commerceApi.reducer,
    },
    middleware: (getDefaultMiddleware) => getDefaultMiddleware().concat(commerceApi.middleware),
  })
}

export const store = setupStore()

export type AppStore = ReturnType<typeof setupStore>
export type RootState = ReturnType<AppStore['getState']>
export type AppDispatch = AppStore['dispatch']
