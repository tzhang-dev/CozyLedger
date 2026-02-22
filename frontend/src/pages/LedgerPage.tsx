import { useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Group, NumberInput, Select, Switch, TextInput } from '@mantine/core'
import { IconChevronLeft, IconChevronRight } from '@tabler/icons-react'
import { useTranslation } from 'react-i18next'
import { CurrencySelect } from '../components/CurrencySelect'
import { createTransaction, listAccounts, listBooks, listCategories, listTransactions, updateTransaction } from '../lib/cozyApi'
import { TransactionType } from '../lib/types'
import type { TransactionResponse } from '../lib/types'

type Props = {
  token: string
  bookId: string
  mode?: 'list' | 'new'
}

type LedgerFilter = 'all' | 'income' | 'expense' | 'transfer'

/**
 * Manages transaction create/update flows and renders the ledger list.
 */
export function LedgerPage({ token, bookId, mode = 'list' }: Props) {
  const { t, i18n } = useTranslation()
  const queryClient = useQueryClient()
  const [selectedTransactionId, setSelectedTransactionId] = useState<string | null>(null)
  const [type, setType] = useState(String(TransactionType.Expense))
  const [dateUtc, setDateUtc] = useState(new Date().toISOString().slice(0, 10))
  const [amount, setAmount] = useState<number>(0)
  const [currency, setCurrency] = useState('')
  const [accountId, setAccountId] = useState<string | null>(null)
  const [toAccountId, setToAccountId] = useState<string | null>(null)
  const [categoryId, setCategoryId] = useState<string | null>(null)
  const [note, setNote] = useState('')
  const [isRefund, setIsRefund] = useState(false)
  const [searchText, setSearchText] = useState('')
  const [filter, setFilter] = useState<LedgerFilter>('all')
  const [currentDate, setCurrentDate] = useState(new Date())

  const transactionTypeOptions = [
    { label: t('typeExpense'), value: String(TransactionType.Expense) },
    { label: t('typeIncome'), value: String(TransactionType.Income) },
    { label: t('typeTransfer'), value: String(TransactionType.Transfer) },
    { label: t('typeBalanceAdjustment'), value: String(TransactionType.BalanceAdjustment) }
  ]

  const accountsQuery = useQuery({
    queryKey: ['accounts', bookId],
    queryFn: () => listAccounts(token, bookId)
  })

  const categoriesQuery = useQuery({
    queryKey: ['categories', bookId],
    queryFn: () => listCategories(token, bookId)
  })

  const booksQuery = useQuery({
    queryKey: ['books', token],
    queryFn: () => listBooks(token)
  })

  const transactionsQuery = useQuery({
    queryKey: ['transactions', bookId],
    queryFn: () => listTransactions(token, bookId)
  })

  const selectedTransaction = useMemo(
    () => transactionsQuery.data?.find((item) => item.id === selectedTransactionId),
    [transactionsQuery.data, selectedTransactionId]
  )

  const bookBaseCurrency = booksQuery.data?.find((book) => book.id === bookId)?.baseCurrency.toUpperCase() ?? 'USD'
  const effectiveCurrency = (currency || bookBaseCurrency).toUpperCase()

  const accountOptions = (accountsQuery.data ?? []).map((account) => ({
    label: `${i18n.language === 'zh' ? account.nameZhHans : account.nameEn} (${account.currency})`,
    value: account.id
  }))

  const categoryOptions = (categoriesQuery.data ?? []).map((category) => ({
    label: i18n.language === 'zh' ? category.nameZhHans : category.nameEn,
    value: category.id
  }))

  const filteredTransactions = useMemo(() => {
    const month = currentDate.getUTCMonth()
    const year = currentDate.getUTCFullYear()

    return (transactionsQuery.data ?? [])
      .filter((item) => {
        const d = new Date(item.dateUtc)
        return d.getUTCMonth() === month && d.getUTCFullYear() === year
      })
      .filter((item) => {
        if (filter === 'all') {
          return true
        }
        if (filter === 'income') {
          return item.type === TransactionType.Income
        }
        if (filter === 'expense') {
          return item.type === TransactionType.Expense
        }
        return item.type === TransactionType.Transfer
      })
      .filter((item) => {
        if (!searchText.trim()) {
          return true
        }

        const keyword = searchText.toLowerCase()
        return (
          (item.note ?? '').toLowerCase().includes(keyword) ||
          item.currency.toLowerCase().includes(keyword) ||
          item.dateUtc.toLowerCase().includes(keyword)
        )
      })
      .sort((a, b) => b.dateUtc.localeCompare(a.dateUtc))
  }, [transactionsQuery.data, currentDate, filter, searchText])

  const groupedTransactions = useMemo(() => {
    return filteredTransactions.reduce<Record<string, TransactionResponse[]>>((acc, item) => {
      const key = item.dateUtc.slice(0, 10)
      if (!acc[key]) {
        acc[key] = []
      }
      acc[key].push(item)
      return acc
    }, {})
  }, [filteredTransactions])

  const monthIncome = filteredTransactions
    .filter((item) => item.type === TransactionType.Income)
    .reduce((sum, item) => sum + item.amount, 0)
  const monthExpense = filteredTransactions
    .filter((item) => item.type === TransactionType.Expense)
    .reduce((sum, item) => sum + item.amount, 0)

  const upsertTransaction = useMutation({
    mutationFn: async () => {
      if (!accountId) {
        throw new Error('Account is required.')
      }

      const payload = {
        type: Number(type) as TransactionType,
        dateUtc: new Date(`${dateUtc}T00:00:00.000Z`).toISOString(),
        amount,
        currency: effectiveCurrency,
        accountId,
        toAccountId,
        categoryId,
        memberId: null,
        note,
        isRefund
      }

      if (selectedTransaction) {
        return updateTransaction(token, bookId, selectedTransaction.id, payload)
      }

      return createTransaction(token, bookId, payload)
    },
    onSuccess: async () => {
      resetForm()
      await queryClient.invalidateQueries({ queryKey: ['transactions', bookId] })
    }
  })

  const resetForm = () => {
    setSelectedTransactionId(null)
    setType(String(TransactionType.Expense))
    setDateUtc(new Date().toISOString().slice(0, 10))
    setAmount(0)
    setCurrency('')
    setAccountId(null)
    setToAccountId(null)
    setCategoryId(null)
    setNote('')
    setIsRefund(false)
  }

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    upsertTransaction.mutate()
  }

  const startEdit = (transaction: TransactionResponse) => {
    setSelectedTransactionId(transaction.id)
    setType(String(transaction.type))
    setDateUtc(transaction.dateUtc.slice(0, 10))
    setAmount(transaction.amount)
    setCurrency(transaction.currency)
    setAccountId(transaction.accountId)
    setToAccountId(transaction.toAccountId ?? null)
    setCategoryId(transaction.categoryId ?? null)
    setNote(transaction.note ?? '')
    setIsRefund(transaction.isRefund)
  }

  const isTransfer = Number(type) === TransactionType.Transfer
  const needsCategory = Number(type) === TransactionType.Expense || Number(type) === TransactionType.Income
  const monthLabel = currentDate.toLocaleDateString(i18n.language === 'zh' ? 'zh-CN' : 'en-US', { month: 'long', year: 'numeric' })
  const isListMode = mode === 'list'

  return (
    <section className="cl-page">
      <header className="cl-header">
        <div className="cl-header-inner">
          <h1 className="cl-header-title">{isListMode ? t('ledgerListTitle') : t('ledgerNewTitle')}</h1>
          <div className="cl-month-nav">
            <button type="button" className="cl-month-button" onClick={() => setCurrentDate(new Date(currentDate.getUTCFullYear(), currentDate.getUTCMonth() - 1, 1))}>
              <IconChevronLeft size={18} />
            </button>
            <span className="cl-month-label">{monthLabel}</span>
            <button type="button" className="cl-month-button" onClick={() => setCurrentDate(new Date(currentDate.getUTCFullYear(), currentDate.getUTCMonth() + 1, 1))}>
              <IconChevronRight size={18} />
            </button>
          </div>
          <div className="cl-stat-grid-3">
            <div className="cl-stat-box cl-stat-box-income">
              <span className="cl-stat-label">{t('summaryIncome')}</span>
              <span className="cl-stat-value cl-amount-income">{bookBaseCurrency} {monthIncome.toFixed(2)}</span>
            </div>
            <div className="cl-stat-box cl-stat-box-expense">
              <span className="cl-stat-label">{t('summaryExpense')}</span>
              <span className="cl-stat-value cl-amount-expense">{bookBaseCurrency} {monthExpense.toFixed(2)}</span>
            </div>
            <div className="cl-stat-box cl-stat-box-net">
              <span className="cl-stat-label">{t('summaryNet')}</span>
              <span className="cl-stat-value">{bookBaseCurrency} {(monthIncome - monthExpense).toFixed(2)}</span>
            </div>
          </div>
        </div>
      </header>

      <div className="cl-body">
        {!isListMode ? (
          <div className="cl-card">
            <form onSubmit={handleSubmit} className="cl-form-grid">
              <Select label={t('typeLabel')} data={transactionTypeOptions} value={type} onChange={(value) => setType(value ?? String(TransactionType.Expense))} />
              <TextInput label={t('dateUtcLabel')} type="date" value={dateUtc} onChange={(event) => setDateUtc(event.currentTarget.value)} required />
              <NumberInput label={t('amountLabel')} value={amount} onChange={(value) => setAmount(Number(value ?? 0))} required />
              <CurrencySelect
                label={t('currencyLabel')}
                value={effectiveCurrency}
                onChange={setCurrency}
                baseCurrency={bookBaseCurrency}
                knownCurrencies={(accountsQuery.data ?? []).map((item) => item.currency)}
                required
              />
              <Select label={t('accountLabel')} data={accountOptions} value={accountId} onChange={setAccountId} searchable required />
              <Select label={t('destinationAccountLabel')} data={accountOptions} value={toAccountId} onChange={setToAccountId} disabled={!isTransfer} clearable />
              <Select label={t('categoryLabel')} data={categoryOptions} value={categoryId} onChange={setCategoryId} disabled={!needsCategory} clearable />
              <TextInput label={t('noteLabel')} value={note} onChange={(event) => setNote(event.currentTarget.value)} />
              <Switch label={t('refundLabel')} checked={isRefund} onChange={(event) => setIsRefund(event.currentTarget.checked)} disabled={Number(type) !== TransactionType.Expense} />
              <Group>
                <Button type="submit" loading={upsertTransaction.isPending}>
                  {selectedTransaction ? t('saveTransaction') : t('addTransaction')}
                </Button>
                {selectedTransaction ? (
                  <Button variant="default" type="button" onClick={resetForm}>
                    {t('cancelEdit')}
                  </Button>
                ) : null}
              </Group>
            </form>
          </div>
        ) : null}

        <div className="cl-card">
          <input
            className="cl-search"
            value={searchText}
            onChange={(event) => setSearchText(event.currentTarget.value)}
            placeholder={t('ledgerSearchPlaceholder')}
          />

          <div className="cl-filter-row" style={{ marginTop: 10 }}>
            {(['all', 'income', 'expense', 'transfer'] as const).map((option) => (
              <button
                key={option}
                type="button"
                className={`cl-chip ${filter === option ? 'cl-chip-active' : ''}`}
                onClick={() => setFilter(option)}
              >
                {option === 'all'
                  ? t('ledgerFilterAll')
                  : option === 'income'
                    ? t('typeIncome')
                    : option === 'expense'
                      ? t('typeExpense')
                      : t('typeTransfer')}
              </button>
            ))}
          </div>

          <div className="cl-list" style={{ marginTop: 10 }}>
            {Object.entries(groupedTransactions).map(([date, items]) => (
              <div key={date} className="cl-list-group">
                <p className="cl-list-group-label">{date}</p>
                {items.map((item) => (
                  <div key={item.id} className="cl-list-row">
                    <div className="cl-list-row-main">
                      <span className="cl-list-row-title">{item.note || t('homeFallbackTransaction')}</span>
                      <span className="cl-list-row-meta">{item.currency} {item.amount.toFixed(2)}</span>
                    </div>
                    <Group gap={8}>
                      <span className={item.type === TransactionType.Income ? 'cl-amount-income' : 'cl-amount-expense'}>
                        {item.type === TransactionType.Income ? '+' : '-'} {item.currency} {item.amount.toFixed(2)}
                      </span>
                      {!isListMode ? (
                        <Button size="xs" variant="light" onClick={() => startEdit(item)}>
                          {t('editButton')}
                        </Button>
                      ) : null}
                    </Group>
                  </div>
                ))}
              </div>
            ))}
            {!filteredTransactions.length ? <p className="cl-empty">{t('ledgerNoResults')}</p> : null}
          </div>
        </div>
      </div>
    </section>
  )
}
