interface AsyncStateProps {
  kind: 'loading' | 'error' | 'empty'
  title: string
  detail?: string
  traceId?: string
  onRetry?: () => void
  retryLabel?: string
}

export function AsyncState({ kind, title, detail, traceId, onRetry, retryLabel = 'Retry' }: AsyncStateProps) {
  if (kind === 'loading') {
    return (
      <div className="async-state" role="status" aria-live="polite">
        <span className="spinner" aria-hidden="true" />
        <p>{title}</p>
      </div>
    )
  }

  if (kind === 'error') {
    return (
      <div className="async-state async-state--error" role="alert">
        <p className="async-state__title">{title}</p>
        {detail ? <p className="async-state__detail">{detail}</p> : null}
        {onRetry ? (
          <button type="button" className="button button--secondary" onClick={onRetry}>
            {retryLabel}
          </button>
        ) : null}
        {traceId ? <p className="async-state__trace">Reference: {traceId}</p> : null}
      </div>
    )
  }

  return (
    <div className="async-state async-state--empty">
      <p>{title}</p>
      {detail ? <p className="async-state__detail">{detail}</p> : null}
    </div>
  )
}
