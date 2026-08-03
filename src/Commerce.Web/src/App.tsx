import { CartPanel } from './features/cart/CartPanel'
import { useCartSession } from './features/cart/useCartSession'
import { ProductCatalog } from './features/products/ProductCatalog'

function App() {
  const session = useCartSession()
  const itemCount = session.data?.items.reduce((total, item) => total + item.quantity, 0) ?? 0

  return (
    <>
      <header className="app-header">
        <div className="app-header__content">
          <div>
            <h1>Commerce Cart</h1>
            <p className="app-header__subtitle">Prices and totals shown are supplied by the server.</p>
          </div>
          <div className="cart-badge">
            <span className="cart-badge__label">Cart</span>
            <span className="cart-badge__count" aria-label={`${itemCount} item${itemCount === 1 ? '' : 's'} in cart`}>
              {itemCount}
            </span>
          </div>
        </div>
      </header>
      <main className="app-main">
        <ProductCatalog cartId={session.data?.id} />
        <CartPanel />
      </main>
    </>
  )
}

export default App
