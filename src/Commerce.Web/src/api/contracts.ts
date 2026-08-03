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
