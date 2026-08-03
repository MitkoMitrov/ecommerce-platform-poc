export interface Product {
  id: string
  name: string
  unitPrice: number
  currency: string
}

export interface CartItem {
  productId: string
  productName: string
  unitPrice: number
  currency: string
  quantity: number
  lineTotal: number
}

export interface Cart {
  id: string
  createdAtUtc: string
  updatedAtUtc: string
  currency: string | null
  subtotal: number
  items: CartItem[]
}

export interface AddCartItemRequest {
  productId: string
  quantity: number
}

export interface UpdateCartItemRequest {
  quantity: number
}

export interface PurchaseItem {
  productId: string
  productName: string
  unitPrice: number
  currency: string
  quantity: number
  lineTotal: number
}

export interface Purchase {
  id: string
  cartId: string
  purchasedAtUtc: string
  currency: string
  total: number
  items: PurchaseItem[]
}

export interface PurchaseCartResponse {
  purchase: Purchase
  cart: Cart
}
