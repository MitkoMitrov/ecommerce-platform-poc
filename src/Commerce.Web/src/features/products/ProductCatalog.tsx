import { useGetProductsQuery } from '../../api/commerceApiSlice'
import { AsyncState } from '../../components/AsyncState'
import { describeError } from '../../lib/errorMessage'
import { ProductCard } from './ProductCard'

interface ProductCatalogProps {
  cartId: string | undefined
}

export function ProductCatalog({ cartId }: ProductCatalogProps) {
  const query = useGetProductsQuery()

  return (
    <section className="panel" aria-labelledby="products-heading">
      <h2 id="products-heading">Products</h2>

      {query.isLoading ? <AsyncState kind="loading" title="Loading products…" /> : null}

      {query.isError
        ? (() => {
            const info = describeError(query.error, 'Unable to load products')
            return (
              <AsyncState
                kind="error"
                title={info.title}
                detail={info.detail}
                traceId={info.traceId}
                onRetry={() => query.refetch()}
                retryLabel="Retry loading products"
              />
            )
          })()
        : null}

      {query.isSuccess && query.data.length === 0 ? (
        <AsyncState kind="empty" title="No products are available right now." />
      ) : null}

      {query.isSuccess && query.data.length > 0 ? (
        <ul className="product-grid">
          {query.data.map((product) => (
            <li key={product.id}>
              <ProductCard product={product} cartId={cartId} />
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  )
}
