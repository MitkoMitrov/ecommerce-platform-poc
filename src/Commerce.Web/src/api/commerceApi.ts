import { apiClient } from './apiClient'
import type {
  AddCartItemRequest,
  Cart,
  Product,
  Purchase,
  PurchaseCartResponse,
  UpdateCartItemRequest,
} from './contracts'

export function getProducts(signal?: AbortSignal): Promise<Product[]> {
  return apiClient.get<Product[]>('/api/products', signal)
}

export function createCart(signal?: AbortSignal): Promise<Cart> {
  return apiClient.post<Cart>('/api/carts', undefined, signal)
}

export function getCart(cartId: string, signal?: AbortSignal): Promise<Cart> {
  return apiClient.get<Cart>(`/api/carts/${cartId}`, signal)
}

export function addCartItem(
  cartId: string,
  request: AddCartItemRequest,
  signal?: AbortSignal,
): Promise<Cart> {
  return apiClient.post<Cart>(`/api/carts/${cartId}/items`, request, signal)
}

export function updateCartItemQuantity(
  cartId: string,
  productId: string,
  request: UpdateCartItemRequest,
  signal?: AbortSignal,
): Promise<Cart> {
  return apiClient.put<Cart>(`/api/carts/${cartId}/items/${productId}`, request, signal)
}

export function removeCartItem(
  cartId: string,
  productId: string,
  signal?: AbortSignal,
): Promise<void> {
  return apiClient.delete<void>(`/api/carts/${cartId}/items/${productId}`, signal)
}

export function purchaseCart(cartId: string, signal?: AbortSignal): Promise<PurchaseCartResponse> {
  return apiClient.post<PurchaseCartResponse>(`/api/carts/${cartId}/purchase`, undefined, signal)
}

export function getPurchaseHistory(signal?: AbortSignal): Promise<Purchase[]> {
  return apiClient.get<Purchase[]>('/api/purchases', signal)
}
