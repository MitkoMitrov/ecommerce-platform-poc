import { StrictMode } from 'react'
import { describe, expect, it } from 'vitest'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Provider } from 'react-redux'
import { delay, http, HttpResponse } from 'msw'
import App from './App'
import { setupStore } from './app/store'
import type { Cart } from './api/contracts'
import { formatMoney } from './lib/currency'
import { activeProducts, problem } from './test/handlers'
import { server } from './test/server'

const CART_STORAGE_KEY = 'commerce.cartId'

function renderApp() {
  const store = setupStore()

  return render(
    <Provider store={store}>
      <App />
    </Provider>,
  )
}

async function seedExistingCart(items: Array<{ productId: string; quantity: number }> = []): Promise<string> {
  const createResponse = await fetch('/api/carts', { method: 'POST' })
  const cart = (await createResponse.json()) as Cart

  for (const item of items) {
    await fetch(`/api/carts/${cart.id}/items`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(item),
    })
  }

  return cart.id
}

describe('Commerce Cart application', () => {
  it('shows an initial loading state', () => {
    renderApp()

    expect(screen.getAllByRole('status').length).toBeGreaterThan(0)
  })

  it('renders products after successful loading, in API order', async () => {
    renderApp()

    const headings = await screen.findAllByRole('heading', { level: 3 })
    expect(headings.map((heading) => heading.textContent)).toEqual(
      activeProducts.map((product) => product.name),
    )
  })

  it('formats product prices using currency-aware formatting', async () => {
    renderApp()

    const keyboard = activeProducts.find((product) => product.name === 'Mechanical Keyboard')
    if (!keyboard) {
      throw new Error('Test fixture "Mechanical Keyboard" was not found in activeProducts.')
    }
    expect(await screen.findByText(formatMoney(keyboard.unitPrice, keyboard.currency))).toBeInTheDocument()
  })

  it('creates a Cart when localStorage has no Cart ID', async () => {
    expect(window.localStorage.getItem(CART_STORAGE_KEY)).toBeNull()

    renderApp()

    await screen.findByText(/Your cart is empty\./)
    expect(window.localStorage.getItem(CART_STORAGE_KEY)).not.toBeNull()
  })

  it('stores the newly created Cart ID in localStorage', async () => {
    renderApp()

    const cartIdText = await screen.findByText(/Cart ID:/)
    const storedId = window.localStorage.getItem(CART_STORAGE_KEY)
    expect(storedId).not.toBeNull()
    expect(cartIdText.textContent).toContain(storedId)
  })

  it('loads a saved Cart rather than creating a new one', async () => {
    const productId = activeProducts[0].id
    const cartId = await seedExistingCart([{ productId, quantity: 2 }])
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)

    renderApp()

    await screen.findByText(activeProducts[0].name, { selector: '.cart-item__name' })
    expect(window.localStorage.getItem(CART_STORAGE_KEY)).toBe(cartId)
  })

  it('replaces a stale saved Cart that returns 404 with a new Cart', async () => {
    window.localStorage.setItem(CART_STORAGE_KEY, 'stale-cart-id')

    renderApp()

    await screen.findByText(/Your cart is empty\./)
    const storedId = window.localStorage.getItem(CART_STORAGE_KEY)
    expect(storedId).not.toBe('stale-cart-id')
    expect(storedId).not.toBeNull()
  })

  it('adds a Product to the Cart', async () => {
    const user = userEvent.setup()
    renderApp()

    const product = activeProducts[0]
    const addButton = await screen.findByRole('button', { name: `Add ${product.name} to cart` })
    await user.click(addButton)

    await screen.findByText(product.name, { selector: '.cart-item__name' })
  })

  it('sends only Product ID and quantity in the add-item request', async () => {
    let capturedBody: unknown = null
    const product = activeProducts[0]

    server.use(
      http.post('/api/carts/:cartId/items', async ({ request }) => {
        capturedBody = await request.json()
        return HttpResponse.json({
          id: 'cart-capture',
          createdAtUtc: '2026-01-01T00:00:00.000Z',
          updatedAtUtc: '2026-01-01T00:00:00.000Z',
          currency: product.currency,
          subtotal: product.unitPrice,
          items: [
            {
              productId: product.id,
              productName: product.name,
              unitPrice: product.unitPrice,
              currency: product.currency,
              quantity: 1,
              lineTotal: product.unitPrice,
            },
          ],
        })
      }),
    )

    const user = userEvent.setup()
    renderApp()

    const addButton = await screen.findByRole('button', { name: `Add ${product.name} to cart` })
    await user.click(addButton)

    await waitFor(() => expect(capturedBody).not.toBeNull())
    expect(capturedBody).toEqual({ productId: product.id, quantity: 1 })
  })

  it('displays cart values exactly as returned by the backend, not recomputed client-side', async () => {
    const product = activeProducts[0]

    server.use(
      http.post('/api/carts/:cartId/items', () =>
        HttpResponse.json({
          id: 'cart-authoritative',
          createdAtUtc: '2026-01-01T00:00:00.000Z',
          updatedAtUtc: '2026-01-01T00:00:00.000Z',
          currency: product.currency,
          subtotal: 777.77,
          items: [
            {
              productId: product.id,
              productName: product.name,
              unitPrice: product.unitPrice,
              currency: product.currency,
              quantity: 1,
              lineTotal: 777.77,
            },
          ],
        }),
      ),
    )

    const user = userEvent.setup()
    renderApp()

    const addButton = await screen.findByRole('button', { name: `Add ${product.name} to cart` })
    await user.click(addButton)

    const matches = await screen.findAllByText(formatMoney(777.77, product.currency))
    expect(matches.length).toBeGreaterThan(0)
  })

  it('increments item quantity', async () => {
    const productId = activeProducts[0].id
    const cartId = await seedExistingCart([{ productId, quantity: 1 }])
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)

    const user = userEvent.setup()
    renderApp()

    const incrementButton = await screen.findByRole('button', {
      name: `Increase quantity of ${activeProducts[0].name}`,
    })
    await user.click(incrementButton)

    await waitFor(() =>
      expect(screen.getByText('2', { selector: '.cart-item__quantity-value' })).toBeInTheDocument(),
    )
  })

  it('decrements item quantity', async () => {
    const productId = activeProducts[0].id
    const cartId = await seedExistingCart([{ productId, quantity: 2 }])
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)

    const user = userEvent.setup()
    renderApp()

    const decrementButton = await screen.findByRole('button', {
      name: `Decrease quantity of ${activeProducts[0].name}`,
    })
    await user.click(decrementButton)

    await waitFor(() =>
      expect(screen.getByText('1', { selector: '.cart-item__quantity-value' })).toBeInTheDocument(),
    )
  })

  it('disables decrement at quantity 1', async () => {
    const productId = activeProducts[0].id
    const cartId = await seedExistingCart([{ productId, quantity: 1 }])
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)

    renderApp()

    const decrementButton = await screen.findByRole('button', {
      name: `Decrease quantity of ${activeProducts[0].name}`,
    })
    expect(decrementButton).toBeDisabled()
  })

  it('disables increment at quantity 99', async () => {
    const productId = activeProducts[0].id
    const cartId = await seedExistingCart([{ productId, quantity: 99 }])
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)

    renderApp()

    const incrementButton = await screen.findByRole('button', {
      name: `Increase quantity of ${activeProducts[0].name}`,
    })
    expect(incrementButton).toBeDisabled()
  })

  it('removes a Cart item', async () => {
    const productId = activeProducts[0].id
    const cartId = await seedExistingCart([{ productId, quantity: 1 }])
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)

    const user = userEvent.setup()
    renderApp()

    const removeButton = await screen.findByRole('button', {
      name: `Remove ${activeProducts[0].name} from cart`,
    })
    await user.click(removeButton)

    await screen.findByText(/Your cart is empty\./)
  })

  it('displays the empty Cart state', async () => {
    renderApp()

    expect(await screen.findByText(/Your cart is empty\./)).toBeInTheDocument()
  })

  it('shows a Retry action when Product loading fails', async () => {
    server.use(http.get('/api/products', () => problem(500, 'An unexpected error occurred')))

    renderApp()

    expect(await screen.findByRole('button', { name: 'Retry loading products' })).toBeInTheDocument()
  })

  it('shows a Retry action when Cart initialization fails', async () => {
    server.use(http.post('/api/carts', () => problem(500, 'An unexpected error occurred')))

    renderApp()

    expect(await screen.findByRole('button', { name: 'Retry loading cart' })).toBeInTheDocument()
  })

  it('recovers after retrying a failed Product request', async () => {
    let attempt = 0
    server.use(
      http.get('/api/products', () => {
        attempt += 1
        if (attempt === 1) {
          return problem(500, 'An unexpected error occurred')
        }
        return HttpResponse.json(activeProducts)
      }),
    )

    const user = userEvent.setup()
    renderApp()

    const retryButton = await screen.findByRole('button', { name: 'Retry loading products' })
    await user.click(retryButton)

    expect(await screen.findByRole('heading', { name: activeProducts[0].name })).toBeInTheDocument()
  })

  it('shows a backend ProblemDetails message for a mutation failure', async () => {
    const product = activeProducts[0]
    server.use(
      http.post('/api/carts/:cartId/items', () =>
        problem(400, 'Invalid request', 'Quantity must be between 1 and 99 inclusive.'),
      ),
    )

    const user = userEvent.setup()
    renderApp()

    const addButton = await screen.findByRole('button', { name: `Add ${product.name} to cart` })
    await user.click(addButton)

    expect(await screen.findByText('Quantity must be between 1 and 99 inclusive.')).toBeInTheDocument()
  })

  it('keeps existing Cart data visible after a mutation failure', async () => {
    const [first, second] = activeProducts
    const cartId = await seedExistingCart([{ productId: first.id, quantity: 1 }])
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)

    server.use(http.post('/api/carts/:cartId/items', () => problem(500, 'An unexpected error occurred')))

    const user = userEvent.setup()
    renderApp()

    await screen.findByText(first.name, { selector: '.cart-item__name' })

    const addSecondButton = await screen.findByRole('button', { name: `Add ${second.name} to cart` })
    await user.click(addSecondButton)

    await screen.findByRole('alert')
    expect(screen.getByText(first.name, { selector: '.cart-item__name' })).toBeInTheDocument()
  })

  it('shows the correct total quantity badge', async () => {
    const productId = activeProducts[0].id
    const cartId = await seedExistingCart([{ productId, quantity: 3 }])
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)

    renderApp()

    await screen.findByText(activeProducts[0].name, { selector: '.cart-item__name' })
    expect(screen.getByLabelText('3 items in cart')).toBeInTheDocument()
  })

  it('uses server-returned subtotal and line totals', async () => {
    const productId = activeProducts[0].id
    const cartId = await seedExistingCart([{ productId, quantity: 2 }])
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)

    renderApp()

    const expectedAmount = formatMoney(activeProducts[0].unitPrice * 2, activeProducts[0].currency)
    const cartHeading = await screen.findByRole('heading', { name: 'Your Cart' })
    const cartPanel = cartHeading.closest('section')
    if (!cartPanel) {
      throw new Error('Expected the Cart heading to be inside a <section> panel.')
    }
    await waitFor(() => {
      expect(within(cartPanel).getByText(expectedAmount, { selector: '.cart-item__line-total' })).toBeInTheDocument()
    })
    expect(within(cartPanel).getByText(expectedAmount, { selector: '.cart-summary strong' })).toBeInTheDocument()
  })

  it('exposes accessible names for quantity and remove controls', async () => {
    const productId = activeProducts[0].id
    const cartId = await seedExistingCart([{ productId, quantity: 1 }])
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)

    renderApp()

    expect(
      await screen.findByRole('button', { name: `Decrease quantity of ${activeProducts[0].name}` }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: `Increase quantity of ${activeProducts[0].name}` }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: `Remove ${activeProducts[0].name} from cart` }),
    ).toBeInTheDocument()
  })

  it('does not create duplicate Carts during StrictMode remounting', async () => {
    let cartCreationRequestCount = 0

    server.use(
      http.post('/api/carts', async () => {
        cartCreationRequestCount += 1
        await delay(50)
        return HttpResponse.json(
          {
            id: 'cart-strict-mode',
            createdAtUtc: '2026-01-01T00:00:00.000Z',
            updatedAtUtc: '2026-01-01T00:00:00.000Z',
            currency: null,
            subtotal: 0,
            items: [],
          },
          { status: 201 },
        )
      }),
    )

    const store = setupStore()

    render(
      <StrictMode>
        <Provider store={store}>
          <App />
        </Provider>
      </StrictMode>,
    )

    await screen.findByText(/Your cart is empty\./)

    expect(cartCreationRequestCount).toBe(1)
    expect(window.localStorage.getItem(CART_STORAGE_KEY)).toBe('cart-strict-mode')
  })

  it('displays zero subtotal without inventing currency for an empty Cart', async () => {
    renderApp()

    const cartHeading = await screen.findByRole('heading', { name: 'Your Cart' })
    const cartPanel = cartHeading.closest('section')
    if (!cartPanel) {
      throw new Error('Expected the Cart heading to be inside a <section> panel.')
    }

    await within(cartPanel).findByText(/Your cart is empty\./)

    const subtotalRow = within(cartPanel).getByText('Subtotal').closest('.cart-summary')
    if (!subtotalRow || !(subtotalRow instanceof HTMLElement)) {
      throw new Error('Expected a .cart-summary element containing the Subtotal label.')
    }

    expect(within(subtotalRow).getByText('0.00')).toBeInTheDocument()
    expect(subtotalRow.textContent).not.toContain('EUR')
    expect(subtotalRow.textContent).not.toContain('$')
    expect(subtotalRow.textContent).not.toContain('€')
  })
})

describe('Purchase and Purchase History', () => {
  it('disables Purchase for an empty Cart', async () => {
    renderApp()

    const purchaseButton = await screen.findByRole('button', { name: 'Purchase' })
    expect(purchaseButton).toBeDisabled()
  })

  it('enables Purchase for a non-empty Cart', async () => {
    const productId = activeProducts[0].id
    const cartId = await seedExistingCart([{ productId, quantity: 1 }])
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)

    renderApp()

    const purchaseButton = await screen.findByRole('button', { name: 'Purchase' })
    await waitFor(() => expect(purchaseButton).toBeEnabled())
  })

  it('sends only the Cart ID through the route with no client price/total payload', async () => {
    let capturedMethod = ''
    let capturedBody = ''
    const productId = activeProducts[0].id
    const cartId = await seedExistingCart([{ productId, quantity: 2 }])
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)

    server.use(
      http.post('/api/carts/:cartId/purchase', async ({ request, params }) => {
        capturedMethod = request.method
        capturedBody = await request.text()
        return HttpResponse.json(
          {
            purchase: {
              id: 'purchase-captured',
              cartId: params.cartId,
              purchasedAtUtc: '2026-01-01T00:00:00.000Z',
              currency: 'EUR',
              total: 59.98,
              items: [],
            },
            cart: {
              id: params.cartId,
              createdAtUtc: '2026-01-01T00:00:00.000Z',
              updatedAtUtc: '2026-01-01T00:00:00.000Z',
              currency: null,
              subtotal: 0,
              items: [],
            },
          },
          { status: 201 },
        )
      }),
    )

    const user = userEvent.setup()
    renderApp()

    const purchaseButton = await screen.findByRole('button', { name: 'Purchase' })
    await user.click(purchaseButton)

    await waitFor(() => expect(capturedMethod).toBe('POST'))
    expect(capturedBody).toBe('')
  })

  it('disables Purchase while the request is pending', async () => {
    const productId = activeProducts[0].id
    const cartId = await seedExistingCart([{ productId, quantity: 1 }])
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)

    server.use(
      http.post('/api/carts/:cartId/purchase', async ({ params }) => {
        await delay(50)
        return HttpResponse.json(
          {
            purchase: {
              id: 'purchase-pending',
              cartId: params.cartId,
              purchasedAtUtc: '2026-01-01T00:00:00.000Z',
              currency: 'EUR',
              total: 29.99,
              items: [],
            },
            cart: {
              id: params.cartId,
              createdAtUtc: '2026-01-01T00:00:00.000Z',
              updatedAtUtc: '2026-01-01T00:00:00.000Z',
              currency: null,
              subtotal: 0,
              items: [],
            },
          },
          { status: 201 },
        )
      }),
    )

    const user = userEvent.setup()
    renderApp()

    const purchaseButton = await screen.findByRole('button', { name: 'Purchase' })
    await user.click(purchaseButton)

    expect(await screen.findByRole('button', { name: 'Processing purchase…' })).toBeDisabled()
  })

  it('shows a confirmation and the server-returned empty Cart with the same ID after a successful purchase', async () => {
    const productId = activeProducts[0].id
    const cartId = await seedExistingCart([{ productId, quantity: 2 }])
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)

    const user = userEvent.setup()
    renderApp()

    await screen.findByText(activeProducts[0].name, { selector: '.cart-item__name' })
    const purchaseButton = await screen.findByRole('button', { name: 'Purchase' })
    await user.click(purchaseButton)

    expect(await screen.findByText(/Purchase confirmed/)).toBeInTheDocument()
    await screen.findByText(/Your cart is empty\./)
    expect(window.localStorage.getItem(CART_STORAGE_KEY)).toBe(cartId)
    expect(await screen.findByText(`Cart ID: ${cartId}`)).toBeInTheDocument()
  })

  it('keeps existing Cart items visible and shows ProblemDetails after a failed purchase', async () => {
    const productId = activeProducts[0].id
    const cartId = await seedExistingCart([{ productId, quantity: 1 }])
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)

    server.use(
      http.post('/api/carts/:cartId/purchase', () =>
        problem(409, 'Conflict', 'Cart is empty and cannot be purchased.'),
      ),
    )

    const user = userEvent.setup()
    renderApp()

    await screen.findByText(activeProducts[0].name, { selector: '.cart-item__name' })
    const purchaseButton = await screen.findByRole('button', { name: 'Purchase' })
    await user.click(purchaseButton)

    expect(await screen.findByText('Cart is empty and cannot be purchased.')).toBeInTheDocument()
    expect(screen.getByText(activeProducts[0].name, { selector: '.cart-item__name' })).toBeInTheDocument()
  })

  it('selects the Purchase History tab without a router', async () => {
    const user = userEvent.setup()
    renderApp()

    const historyTab = await screen.findByRole('tab', { name: 'Purchase History' })
    await user.click(historyTab)

    expect(historyTab).toHaveAttribute('aria-selected', 'true')
    expect(await screen.findByRole('heading', { name: 'Purchase History' })).toBeInTheDocument()
  })

  it('shows a loading state for Purchase History', async () => {
    server.use(
      http.get('/api/purchases', async () => {
        await delay(50)
        return HttpResponse.json([])
      }),
    )

    const user = userEvent.setup()
    renderApp()

    const historyTab = await screen.findByRole('tab', { name: 'Purchase History' })
    await user.click(historyTab)

    expect(await screen.findByText('Loading purchase history…')).toBeInTheDocument()
  })

  it('shows "No purchases yet." for empty Purchase History', async () => {
    const user = userEvent.setup()
    renderApp()

    const historyTab = await screen.findByRole('tab', { name: 'Purchase History' })
    await user.click(historyTab)

    expect(await screen.findByText('No purchases yet.')).toBeInTheDocument()
  })

  it('renders persisted Purchase data using server-returned totals', async () => {
    const product = activeProducts[0]
    server.use(
      http.get('/api/purchases', () =>
        HttpResponse.json([
          {
            id: 'purchase-fixed',
            cartId: 'cart-fixed',
            purchasedAtUtc: '2026-01-05T10:00:00.000Z',
            currency: 'EUR',
            total: 777.77,
            items: [
              {
                productId: product.id,
                productName: product.name,
                unitPrice: product.unitPrice,
                currency: 'EUR',
                quantity: 2,
                lineTotal: 777.77,
              },
            ],
          },
        ]),
      ),
    )

    const user = userEvent.setup()
    renderApp()

    const historyTab = await screen.findByRole('tab', { name: 'Purchase History' })
    await user.click(historyTab)

    expect(await screen.findByText('Purchase ID: purchase-fixed')).toBeInTheDocument()
    expect(screen.getByText('Cart ID: cart-fixed')).toBeInTheDocument()
    expect(screen.getByText(product.name)).toBeInTheDocument()
    expect(screen.getAllByText(formatMoney(777.77, 'EUR')).length).toBeGreaterThan(0)
  })

  it('recovers after retrying a failed Purchase History request', async () => {
    let attempt = 0
    server.use(
      http.get('/api/purchases', () => {
        attempt += 1
        if (attempt === 1) {
          return problem(500, 'An unexpected error occurred')
        }
        return HttpResponse.json([])
      }),
    )

    const user = userEvent.setup()
    renderApp()

    const historyTab = await screen.findByRole('tab', { name: 'Purchase History' })
    await user.click(historyTab)

    const retryButton = await screen.findByRole('button', { name: 'Retry loading purchase history' })
    await user.click(retryButton)

    expect(await screen.findByText('No purchases yet.')).toBeInTheDocument()
  })

  it('shows a newly completed purchase in Purchase History after switching tabs', async () => {
    const productId = activeProducts[0].id
    const cartId = await seedExistingCart([{ productId, quantity: 1 }])
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)

    const user = userEvent.setup()
    renderApp()

    await screen.findByText(activeProducts[0].name, { selector: '.cart-item__name' })
    const purchaseButton = await screen.findByRole('button', { name: 'Purchase' })
    await user.click(purchaseButton)
    await screen.findByText(/Purchase confirmed/)

    const historyTab = await screen.findByRole('tab', { name: 'Purchase History' })
    await user.click(historyTab)

    expect(await screen.findByText(activeProducts[0].name)).toBeInTheDocument()
    expect(screen.getByText(`Cart ID: ${cartId}`)).toBeInTheDocument()
  })

  it('exposes accessible names for the Shop/Purchase History tabs and the Purchase control', async () => {
    const productId = activeProducts[0].id
    const cartId = await seedExistingCart([{ productId, quantity: 1 }])
    window.localStorage.setItem(CART_STORAGE_KEY, cartId)

    renderApp()

    expect(await screen.findByRole('tab', { name: 'Shop' })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Purchase History' })).toBeInTheDocument()
    expect(await screen.findByRole('button', { name: 'Purchase' })).toBeInTheDocument()
  })
})
