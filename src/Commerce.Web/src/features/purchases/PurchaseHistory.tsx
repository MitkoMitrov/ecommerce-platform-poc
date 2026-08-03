import { useGetPurchaseHistoryQuery } from '../../api/commerceApiSlice'
import { AsyncState } from '../../components/AsyncState'
import { describeError } from '../../lib/errorMessage'
import { formatMoney } from '../../lib/currency'

export function PurchaseHistory() {
  const query = useGetPurchaseHistoryQuery()

  return (
    <section className="panel" aria-labelledby="purchase-history-heading">
      <h2 id="purchase-history-heading">Purchase History</h2>
      <p className="purchase-history__disclaimer">
        Demo purchases only — no real payment is processed and no order is fulfilled. This history is global
        and unscoped because authentication is outside this proof of concept; a production build would scope
        it to the signed-in user or organization.
      </p>

      {query.isLoading ? <AsyncState kind="loading" title="Loading purchase history…" /> : null}

      {query.isError
        ? (() => {
            const info = describeError(query.error, 'Unable to load purchase history')
            return (
              <AsyncState
                kind="error"
                title={info.title}
                detail={info.detail}
                traceId={info.traceId}
                onRetry={() => query.refetch()}
                retryLabel="Retry loading purchase history"
              />
            )
          })()
        : null}

      {query.isSuccess && query.data.length === 0 ? <AsyncState kind="empty" title="No purchases yet." /> : null}

      {query.isSuccess && query.data.length > 0 ? (
        <ul className="purchase-list">
          {query.data.map((purchase) => (
            <li key={purchase.id} className="purchase-card">
              <div className="purchase-card__header">
                <p className="purchase-card__date">{new Date(purchase.purchasedAtUtc).toLocaleString()}</p>
                <strong className="purchase-card__total">{formatMoney(purchase.total, purchase.currency)}</strong>
              </div>
              <p className="purchase-card__meta">Purchase ID: {purchase.id}</p>
              <p className="purchase-card__meta">Cart ID: {purchase.cartId}</p>
              <ul className="purchase-card__items">
                {purchase.items.map((item) => (
                  <li key={item.productId} className="purchase-card__item">
                    <span className="purchase-card__item-name">{item.productName}</span>
                    <span className="purchase-card__item-quantity">Qty {item.quantity}</span>
                    <span className="purchase-card__item-unit-price">
                      {formatMoney(item.unitPrice, item.currency)} each
                    </span>
                    <span className="purchase-card__item-line-total">
                      {formatMoney(item.lineTotal, item.currency)}
                    </span>
                  </li>
                ))}
              </ul>
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  )
}
