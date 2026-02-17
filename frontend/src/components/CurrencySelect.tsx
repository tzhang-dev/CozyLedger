import { Select } from '@mantine/core'

const defaultCurrencyOptions = ['USD', 'CNY', 'EUR', 'JPY', 'GBP', 'HKD', 'CAD', 'AUD', 'SGD']

type Props = {
  label: string
  value: string
  onChange: (value: string) => void
  baseCurrency: string
  knownCurrencies?: string[]
  required?: boolean
}

/**
 * Shared currency selector that enforces dropdown-based currency selection.
 */
export function CurrencySelect({ label, value, onChange, baseCurrency, knownCurrencies = [], required = false }: Props) {
  const values = new Set<string>(defaultCurrencyOptions)
  values.add(baseCurrency.toUpperCase())
  values.add(value.toUpperCase())

  for (const item of knownCurrencies) {
    values.add(item.toUpperCase())
  }

  const options = [...values].sort().map((item) => ({ label: item, value: item }))

  return (
    <Select
      label={label}
      data={options}
      value={value.toUpperCase()}
      onChange={(next) => onChange((next ?? baseCurrency).toUpperCase())}
      searchable
      required={required}
    />
  )
}
