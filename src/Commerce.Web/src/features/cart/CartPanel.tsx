import type { Cart } from '../../api/contracts'
import { usePurchaseCartMutation } from '../../api/commerceApiSlice'
import { AsyncState } from '../../components/AsyncState'
import { describeError } from '../../lib/errorMessage'
import { formatMoney } from '../../lib/currency'
import { CartItemRow } from './CartItemRow'
import { useCartSession } from './useCartSession'

export function CartPanel() {
  const session = useCartSession()

  return (
    <section className="panel cart-panel" aria-labelledby="cart-heading">
      <h2 id="cart-heading">Your Cart</h2>

      {session.isLoading ? <AsyncState kind="loading" title="Loading your cart…" /> : null}

      {session.isError
        ? (() => {
            const info = describeError(session.error, 'Unable to load your cart')
            return (
              <AsyncState
                kind="error"
                title={info.title}
                detail={info.detail}
                traceId={info.traceId}
                onRetry={() => session.refetch()}
                retryLabel="Retry loading cart"
              />
            )
          })()
        : null}

      {session.isSuccess && session.data ? <CartContents cart={session.data} /> : null}
    </section>
  )
}

function CartContents({ cart }: { cart: Cart }) {
  const isEmpty = cart.items.length === 0
  const [purchaseCart, purchaseResult] = usePurchaseCartMutation()

  const handlePurchase = () => {
    if (isEmpty || purchaseResult.isLoading) {
      return
    }
    purchaseCart({ cartId: cart.id })
  }

  const purchaseErrorInfo = purchaseResult.isError
    ? describeError(purchaseResult.error, 'Could not complete purchase')
    : null

  return (
    <>
      {isEmpty ? (
        <AsyncState kind="empty" title="Your cart is empty." detail="Add a product to get started." />
      ) : (
        <ul className="cart-items">
          {cart.items.map((item) => (
            <CartItemRow key={item.productId} item={item} cartId={cart.id} />
          ))}
        </ul>
      )}
      <div className="cart-summary">
        <span>Subtotal</span>
        <strong>{formatMoney(cart.subtotal, cart.currency)}</strong>
      </div>

      <div className="cart-actions">
        <button
          type="button"
          className="button button--primary cart-purchase-button"
          onClick={handlePurchase}
          disabled={isEmpty || purchaseResult.isLoading}
        >
          {purchaseResult.isLoading ? 'Processing purchase…' : 'Purchase'}
        </button>

        {purchaseResult.isLoading ? (
          <span className="visually-hidden" role="status">
            Processing purchase…
          </span>
        ) : null}

        {purchaseResult.isSuccess && isEmpty ? (
          <p role="status" className="purchase-confirmation">
            Purchase confirmed — this is a demo record, no real payment was processed. See it under Purchase
            History.
          </p>
        ) : null}

        {purchaseErrorInfo ? (
          <p role="alert" className="field-error cart-purchase-error">
            {purchaseErrorInfo.detail ?? purchaseErrorInfo.title}
          </p>
        ) : null}
      </div>

      <p className="cart-id">Cart ID: {cart.id}</p>
    </>
  )
}
