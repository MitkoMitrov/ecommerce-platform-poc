import type { CartItem } from '../../api/contracts'
import { useRemoveCartItemMutation, useUpdateCartItemQuantityMutation } from '../../api/commerceApiSlice'
import { formatMoney } from '../../lib/currency'
import { describeError } from '../../lib/errorMessage'

interface CartItemRowProps {
  item: CartItem
  cartId: string
}

const MIN_QUANTITY = 1
const MAX_QUANTITY = 99

export function CartItemRow({ item, cartId }: CartItemRowProps) {
  const [updateQuantity, updateQuantityResult] = useUpdateCartItemQuantityMutation()
  const [removeItem, removeItemResult] = useRemoveCartItemMutation()

  const isBusy = updateQuantityResult.isLoading || removeItemResult.isLoading

  const handleDecrement = () => {
    if (isBusy || item.quantity <= MIN_QUANTITY) {
      return
    }
    updateQuantity({ cartId, productId: item.productId, quantity: item.quantity - 1 })
  }

  const handleIncrement = () => {
    if (isBusy || item.quantity >= MAX_QUANTITY) {
      return
    }
    updateQuantity({ cartId, productId: item.productId, quantity: item.quantity + 1 })
  }

  const handleRemove = () => {
    if (isBusy) {
      return
    }
    removeItem({ cartId, productId: item.productId })
  }

  const errorInfo = updateQuantityResult.isError
    ? describeError(updateQuantityResult.error, 'Could not update quantity')
    : removeItemResult.isError
      ? describeError(removeItemResult.error, 'Could not remove item')
      : null

  return (
    <li className="cart-item">
      <div className="cart-item__info">
        <p className="cart-item__name">{item.productName}</p>
        <p className="cart-item__unit-price">{formatMoney(item.unitPrice, item.currency)} each</p>
      </div>

      <div className="cart-item__quantity" role="group" aria-label={`Quantity for ${item.productName}`}>
        <button
          type="button"
          className="button button--icon"
          onClick={handleDecrement}
          disabled={isBusy || item.quantity <= MIN_QUANTITY}
          aria-label={`Decrease quantity of ${item.productName}`}
        >
          &minus;
        </button>
        <span className="cart-item__quantity-value">{item.quantity}</span>
        <button
          type="button"
          className="button button--icon"
          onClick={handleIncrement}
          disabled={isBusy || item.quantity >= MAX_QUANTITY}
          aria-label={`Increase quantity of ${item.productName}`}
        >
          +
        </button>
      </div>

      <p className="cart-item__line-total">{formatMoney(item.lineTotal, item.currency)}</p>

      <button
        type="button"
        className="button button--danger-text"
        onClick={handleRemove}
        disabled={isBusy}
        aria-label={`Remove ${item.productName} from cart`}
      >
        Remove
      </button>

      {isBusy ? (
        <span className="visually-hidden" role="status">
          Updating {item.productName}…
        </span>
      ) : null}

      {errorInfo ? (
        <p role="alert" className="field-error cart-item__error">
          {errorInfo.detail ?? errorInfo.title}
        </p>
      ) : null}
    </li>
  )
}
