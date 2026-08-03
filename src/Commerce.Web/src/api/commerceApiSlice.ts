import { createApi, fakeBaseQuery } from '@reduxjs/toolkit/query/react'
import { ApiError } from './apiClient'
import {
  addCartItem,
  createCart,
  getCart,
  getProducts,
  getPurchaseHistory,
  purchaseCart,
  removeCartItem,
  updateCartItemQuantity,
} from './commerceApi'
import type {
  AddCartItemRequest,
  Cart,
  Product,
  Purchase,
  PurchaseCartResponse,
  UpdateCartItemRequest,
} from './contracts'

const CART_STORAGE_KEY = 'commerce.cartId'

export interface SerializedApiError {
  status: number
  title: string
  detail?: string
  traceId?: string
}

function toSerializedError(error: unknown): SerializedApiError {
  if (error instanceof ApiError) {
    return { status: error.status, title: error.title, detail: error.detail, traceId: error.traceId }
  }
  return { status: 0, title: 'Unexpected error', detail: 'An unexpected error occurred.' }
}

function readStoredCartId(): string | null {
  try {
    return window.localStorage.getItem(CART_STORAGE_KEY)
  } catch {
    return null
  }
}

function storeCartId(cartId: string): void {
  try {
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)
  } catch {
    // Storage may be unavailable; the cart still works for the current page session.
  }
}

function clearStoredCartId(): void {
  try {
    window.localStorage.removeItem(CART_STORAGE_KEY)
  } catch {
    // Ignore inability to clear storage.
  }
}

// Cart creation is a non-idempotent POST. It must never be tied to a cancellable per-request
// signal: an aborted request can still have already reached the server, and re-issuing it on
// abort (e.g. a React StrictMode dev-mode unmount/remount) would create a second Cart. This
// module-scoped promise is shared across concurrent callers — regardless of how many components
// subscribe to the session query, or how many times RTK Query (re-)invokes this queryFn — so at
// most one Cart-creation request is ever in flight at a time.
let cartCreationPromise: Promise<Cart> | null = null

function createAndStoreCart(): Promise<Cart> {
  if (cartCreationPromise) {
    return cartCreationPromise
  }

  cartCreationPromise = (async () => {
    const cart = await createCart()
    storeCartId(cart.id)
    return cart
  })().finally(() => {
    cartCreationPromise = null
  })

  return cartCreationPromise
}

async function ensureCartSession(signal?: AbortSignal): Promise<Cart> {
  const savedCartId = readStoredCartId()

  if (savedCartId) {
    try {
      return await getCart(savedCartId, signal)
    } catch (error) {
      if (!(error instanceof ApiError) || error.status !== 404) {
        throw error
      }
      clearStoredCartId()
    }
  }

  return createAndStoreCart()
}

export const commerceApi = createApi({
  reducerPath: 'commerceApi',
  baseQuery: fakeBaseQuery<SerializedApiError>(),
  tagTypes: ['Cart', 'PurchaseHistory'],
  endpoints: (builder) => ({
    getProducts: builder.query<Product[], void>({
      queryFn: async (_arg, api) => {
        try {
          const data = await getProducts(api.signal)
          return { data }
        } catch (error) {
          return { error: toSerializedError(error) }
        }
      },
    }),

    getCartSession: builder.query<Cart, void>({
      queryFn: async (_arg, api) => {
        try {
          const data = await ensureCartSession(api.signal)
          return { data }
        } catch (error) {
          return { error: toSerializedError(error) }
        }
      },
      providesTags: ['Cart'],
    }),

    addCartItem: builder.mutation<Cart, { cartId: string; productId: string }>({
      queryFn: async ({ cartId, productId }) => {
        try {
          const request: AddCartItemRequest = { productId, quantity: 1 }
          const data = await addCartItem(cartId, request)
          return { data }
        } catch (error) {
          return { error: toSerializedError(error) }
        }
      },
      async onQueryStarted(_arg, { dispatch, queryFulfilled }) {
        try {
          const { data: cart } = await queryFulfilled
          dispatch(commerceApi.util.upsertQueryData('getCartSession', undefined, cart))
        } catch {
          // Mutation failed — leave the cached Cart untouched so the UI keeps showing the
          // last successful state. The error itself is surfaced via the mutation hook's own
          // isError/error result, not through the cache.
        }
      },
    }),

    updateCartItemQuantity: builder.mutation<Cart, { cartId: string; productId: string; quantity: number }>({
      queryFn: async ({ cartId, productId, quantity }) => {
        try {
          const request: UpdateCartItemRequest = { quantity }
          const data = await updateCartItemQuantity(cartId, productId, request)
          return { data }
        } catch (error) {
          return { error: toSerializedError(error) }
        }
      },
      async onQueryStarted(_arg, { dispatch, queryFulfilled }) {
        try {
          const { data: cart } = await queryFulfilled
          dispatch(commerceApi.util.upsertQueryData('getCartSession', undefined, cart))
        } catch {
          // Mutation failed — leave the cached Cart untouched so the UI keeps showing the
          // last successful state. The error itself is surfaced via the mutation hook's own
          // isError/error result, not through the cache.
        }
      },
    }),

    removeCartItem: builder.mutation<void, { cartId: string; productId: string }>({
      queryFn: async ({ cartId, productId }) => {
        try {
          await removeCartItem(cartId, productId)
          return { data: undefined }
        } catch (error) {
          return { error: toSerializedError(error) }
        }
      },
      invalidatesTags: ['Cart'],
    }),

    purchaseCart: builder.mutation<PurchaseCartResponse, { cartId: string }>({
      queryFn: async ({ cartId }) => {
        try {
          const data = await purchaseCart(cartId)
          return { data }
        } catch (error) {
          return { error: toSerializedError(error) }
        }
      },
      invalidatesTags: ['PurchaseHistory'],
      async onQueryStarted(_arg, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled
          dispatch(commerceApi.util.upsertQueryData('getCartSession', undefined, data.cart))
        } catch {
          // Mutation failed — leave the cached Cart untouched so existing items stay visible.
          // The error itself is surfaced via the mutation hook's own isError/error result.
        }
      },
    }),

    getPurchaseHistory: builder.query<Purchase[], void>({
      queryFn: async (_arg, api) => {
        try {
          const data = await getPurchaseHistory(api.signal)
          return { data }
        } catch (error) {
          return { error: toSerializedError(error) }
        }
      },
      providesTags: ['PurchaseHistory'],
    }),
  }),
})

export const {
  useGetProductsQuery,
  useGetCartSessionQuery,
  useAddCartItemMutation,
  useUpdateCartItemQuantityMutation,
  useRemoveCartItemMutation,
  usePurchaseCartMutation,
  useGetPurchaseHistoryQuery,
} = commerceApi
