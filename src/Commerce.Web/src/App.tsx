import { useState } from 'react'
import { CartPanel } from './features/cart/CartPanel'
import { useCartSession } from './features/cart/useCartSession'
import { ProductCatalog } from './features/products/ProductCatalog'
import { PurchaseHistory } from './features/purchases/PurchaseHistory'

type Tab = 'shop' | 'history'

function App() {
  const session = useCartSession()
  const itemCount = session.data?.items.reduce((total, item) => total + item.quantity, 0) ?? 0
  const [activeTab, setActiveTab] = useState<Tab>('shop')

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

      <div className="tabs" role="tablist" aria-label="Sections">
        <button
          type="button"
          role="tab"
          id="shop-tab"
          aria-selected={activeTab === 'shop'}
          aria-controls="shop-panel"
          className={`tab-button${activeTab === 'shop' ? ' tab-button--active' : ''}`}
          onClick={() => setActiveTab('shop')}
        >
          Shop
        </button>
        <button
          type="button"
          role="tab"
          id="history-tab"
          aria-selected={activeTab === 'history'}
          aria-controls="history-panel"
          className={`tab-button${activeTab === 'history' ? ' tab-button--active' : ''}`}
          onClick={() => setActiveTab('history')}
        >
          Purchase History
        </button>
      </div>

      <main className="app-main">
        {activeTab === 'shop' ? (
          <div id="shop-panel" role="tabpanel" aria-labelledby="shop-tab" style={{ display: 'contents' }}>
            <ProductCatalog cartId={session.data?.id} />
            <CartPanel />
          </div>
        ) : (
          <div id="history-panel" role="tabpanel" aria-labelledby="history-tab" className="history-panel">
            <PurchaseHistory />
          </div>
        )}
      </main>
    </>
  )
}

export default App
