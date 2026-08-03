import { http, HttpResponse } from 'msw'
import type { Cart, CartItem, Product, Purchase, PurchaseCartResponse } from '../api/contracts'

export const activeProducts: Product[] = [
  { id: '22222222-2222-2222-2222-222222222222', name: 'Mechanical Keyboard', unitPrice: 89.99, currency: 'EUR' },
  { id: '33333333-3333-3333-3333-333333333333', name: 'USB-C Hub', unitPrice: 49.5, currency: 'EUR' },
  { id: '11111111-1111-1111-1111-111111111111', name: 'Wireless Mouse', unitPrice: 29.99, currency: 'EUR' },
]

interface StoredCart {
  id: string
  createdAtUtc: string
  items: Map<string, number>
}

const carts = new Map<string, StoredCart>()
let cartCounter = 0

const purchases: Purchase[] = []
let purchaseCounter = 0

export function resetMockData(): void {
  carts.clear()
  cartCounter = 0
  purchases.length = 0
  purchaseCounter = 0
}

export function problem(status: number, title: string, detail?: string) {
  return HttpResponse.json(
    { status, title, detail, traceId: 'test-trace-id' },
    { status, headers: { 'Content-Type': 'application/problem+json' } },
  )
}

function toCartResponse(stored: StoredCart): Cart {
  const items: CartItem[] = Array.from(stored.items.entries()).map(([productId, quantity]) => {
    const product = activeProducts.find((candidate) => candidate.id === productId)
    if (!product) {
      throw new Error(`Test fixture inconsistency: product '${productId}' not found in activeProducts.`)
    }
    return {
      productId,
      productName: product.name,
      unitPrice: product.unitPrice,
      currency: product.currency,
      quantity,
      lineTotal: Number((product.unitPrice * quantity).toFixed(2)),
    }
  })

  const currency = items.length > 0 ? items[0].currency : null
  const subtotal = Number(items.reduce((sum, item) => sum + item.lineTotal, 0).toFixed(2))

  return {
    id: stored.id,
    createdAtUtc: stored.createdAtUtc,
    updatedAtUtc: stored.createdAtUtc,
    currency,
    subtotal,
    items,
  }
}

export const handlers = [
  http.get('/api/products', () => HttpResponse.json(activeProducts)),

  http.post('/api/carts', () => {
    cartCounter += 1
    const stored: StoredCart = {
      id: `cart-${cartCounter}`,
      createdAtUtc: '2026-01-01T00:00:00.000Z',
      items: new Map(),
    }
    carts.set(stored.id, stored)
    return HttpResponse.json(toCartResponse(stored), { status: 201 })
  }),

  http.get('/api/carts/:cartId', ({ params }) => {
    const stored = carts.get(params.cartId as string)
    if (!stored) {
      return problem(404, 'Resource not found', `Cart '${params.cartId}' was not found.`)
    }
    return HttpResponse.json(toCartResponse(stored))
  }),

  http.post('/api/carts/:cartId/items', async ({ params, request }) => {
    const stored = carts.get(params.cartId as string)
    if (!stored) {
      return problem(404, 'Resource not found', `Cart '${params.cartId}' was not found.`)
    }
    const body = (await request.json()) as { productId: string; quantity: number }
    const product = activeProducts.find((candidate) => candidate.id === body.productId)
    if (!product) {
      return problem(400, 'Invalid request', `Product '${body.productId}' was not found.`)
    }
    const currentQuantity = stored.items.get(body.productId) ?? 0
    stored.items.set(body.productId, currentQuantity + body.quantity)
    return HttpResponse.json(toCartResponse(stored))
  }),

  http.put('/api/carts/:cartId/items/:productId', async ({ params, request }) => {
    const stored = carts.get(params.cartId as string)
    if (!stored) {
      return problem(404, 'Resource not found', `Cart '${params.cartId}' was not found.`)
    }
    const productId = params.productId as string
    if (!stored.items.has(productId)) {
      return problem(404, 'Resource not found', `Cart item '${productId}' was not found.`)
    }
    const body = (await request.json()) as { quantity: number }
    stored.items.set(productId, body.quantity)
    return HttpResponse.json(toCartResponse(stored))
  }),

  http.delete('/api/carts/:cartId/items/:productId', ({ params }) => {
    const stored = carts.get(params.cartId as string)
    if (!stored) {
      return problem(404, 'Resource not found', `Cart '${params.cartId}' was not found.`)
    }
    stored.items.delete(params.productId as string)
    return new HttpResponse(null, { status: 204 })
  }),

  http.post('/api/carts/:cartId/purchase', ({ params }) => {
    const stored = carts.get(params.cartId as string)
    if (!stored) {
      return problem(404, 'Resource not found', `Cart '${params.cartId}' was not found.`)
    }

    const cartBeforePurchase = toCartResponse(stored)
    if (cartBeforePurchase.items.length === 0) {
      return problem(409, 'Conflict', `Cart '${params.cartId}' is empty and cannot be purchased.`)
    }

    purchaseCounter += 1
    const purchase: Purchase = {
      id: `purchase-${purchaseCounter}`,
      cartId: stored.id,
      purchasedAtUtc: `2026-02-${String(purchaseCounter).padStart(2, '0')}T00:00:00.000Z`,
      currency: cartBeforePurchase.currency ?? 'EUR',
      total: cartBeforePurchase.subtotal,
      items: cartBeforePurchase.items,
    }
    purchases.unshift(purchase)

    stored.items.clear()
    const clearedCart = toCartResponse(stored)

    const response: PurchaseCartResponse = { purchase, cart: clearedCart }
    return HttpResponse.json(response, { status: 201 })
  }),

  http.get('/api/purchases', () => HttpResponse.json(purchases)),
]
