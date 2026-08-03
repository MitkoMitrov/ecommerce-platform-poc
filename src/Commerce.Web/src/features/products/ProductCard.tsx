import type { Product } from '../../api/contracts'
import { useAddCartItemMutation } from '../../api/commerceApiSlice'
import { describeError } from '../../lib/errorMessage'
import { formatMoney } from '../../lib/currency'

interface ProductCardProps {
  product: Product
  cartId: string | undefined
}

export function ProductCard({ product, cartId }: ProductCardProps) {
  const [addItem, addItemResult] = useAddCartItemMutation()

  const handleAdd = () => {
    if (!cartId || addItemResult.isLoading) {
      return
    }
    addItem({ cartId, productId: product.id })
  }

  const errorInfo = addItemResult.isError
    ? describeError(addItemResult.error, 'Could not add item')
    : null

  return (
    <article className="product-card">
      <h3 className="product-card__name">{product.name}</h3>
      <p className="product-card__price">{formatMoney(product.unitPrice, product.currency)}</p>
      <button
        type="button"
        className="button button--primary"
        onClick={handleAdd}
        disabled={!cartId || addItemResult.isLoading}
        aria-label={`Add ${product.name} to cart`}
      >
        {addItemResult.isLoading ? 'Adding…' : 'Add to cart'}
      </button>
      {errorInfo ? (
        <p role="alert" className="field-error">
          {errorInfo.detail ?? errorInfo.title}
        </p>
      ) : null}
    </article>
  )
}
