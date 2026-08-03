export function formatMoney(amount: number, currency: string | null): string {
  if (!currency) {
    return amount.toFixed(2)
  }

  return new Intl.NumberFormat(undefined, {
    style: 'currency',
    currency,
  }).format(amount)
}
