import { useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Card, Group, NumberInput, Select, Stack, Switch, Text, TextInput, Title } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { createTransaction, listAccounts, listCategories, listTransactions, updateTransaction } from '../lib/cozyApi'
import { TransactionType } from '../lib/types'
import type { TransactionResponse } from '../lib/types'

type Props = {
  token: string
  bookId: string
}

/**
 * Manages transaction create/update flows and renders the ledger list.
 */
export function LedgerPage({ token, bookId }: Props) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [selectedTransactionId, setSelectedTransactionId] = useState<string | null>(null)
  const [type, setType] = useState(String(TransactionType.Expense))
  const [dateUtc, setDateUtc] = useState(new Date().toISOString().slice(0, 10))
  const [amount, setAmount] = useState<number>(0)
  const [currency, setCurrency] = useState('USD')
  const [accountId, setAccountId] = useState<string | null>(null)
  const [toAccountId, setToAccountId] = useState<string | null>(null)
  const [categoryId, setCategoryId] = useState<string | null>(null)
  const [note, setNote] = useState('')
  const [isRefund, setIsRefund] = useState(false)

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

  const transactionsQuery = useQuery({
    queryKey: ['transactions', bookId],
    queryFn: () => listTransactions(token, bookId)
  })

  const selectedTransaction = useMemo(
    () => transactionsQuery.data?.find((item) => item.id === selectedTransactionId),
    [transactionsQuery.data, selectedTransactionId]
  )
  const groupedTransactions = useMemo(() => {
    const items = transactionsQuery.data ?? []
    return items.reduce<Record<string, TransactionResponse[]>>((acc, item) => {
      const key = item.dateUtc.slice(0, 10)
      if (!acc[key]) {
        acc[key] = []
      }

      acc[key].push(item)
      return acc
    }, {})
  }, [transactionsQuery.data])

  const accountOptions = (accountsQuery.data ?? []).map((account) => ({
    label: `${account.nameEn} (${account.currency})`,
    value: account.id
  }))

  const categoryOptions = (categoriesQuery.data ?? []).map((category) => ({
    label: category.nameEn,
    value: category.id
  }))

  const upsertTransaction = useMutation({
    mutationFn: async () => {
      if (!accountId) {
        throw new Error('Account is required.')
      }

      const payload = {
        type: Number(type) as TransactionType,
        dateUtc: new Date(`${dateUtc}T00:00:00.000Z`).toISOString(),
        amount,
        currency,
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
    setCurrency('USD')
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
  const getTransactionTypeLabel = (value: TransactionType) => {
    switch (value) {
      case TransactionType.Expense:
        return t('typeExpense')
      case TransactionType.Income:
        return t('typeIncome')
      case TransactionType.Transfer:
        return t('typeTransfer')
      case TransactionType.BalanceAdjustment:
        return t('typeBalanceAdjustment')
      case TransactionType.LiabilityAdjustment:
        return t('typeLiabilityAdjustment')
      default:
        return 'Unknown'
    }
  }

  return (
    <section className="page-panel">
      <div className="page-titlebar">
        <div>
          <Title order={2}>{t('ledgerTitle')}</Title>
          <p>{t('transactionsTitle')}</p>
        </div>
      </div>
      <div className="split-grid">
        <Card shadow="sm" radius="md" className="surface-card">
          <form onSubmit={handleSubmit} className="form-grid">
            <Text fw={600}>{selectedTransaction ? t('editTransaction') : t('createTransaction')}</Text>
            <Select label={t('typeLabel')} data={transactionTypeOptions} value={type} onChange={(value) => setType(value ?? String(TransactionType.Expense))} />
            <TextInput label={t('dateUtcLabel')} value={dateUtc} onChange={(event) => setDateUtc(event.currentTarget.value)} required />
            <NumberInput label={t('amountLabel')} value={amount} onChange={(value) => setAmount(Number(value ?? 0))} required />
            <TextInput label={t('currencyLabel')} value={currency} onChange={(event) => setCurrency(event.currentTarget.value.toUpperCase())} maxLength={3} required />
            <Select label={t('accountLabel')} data={accountOptions} value={accountId} onChange={setAccountId} searchable required />
            <Select
              label={t('destinationAccountLabel')}
              data={accountOptions}
              value={toAccountId}
              onChange={setToAccountId}
              disabled={!isTransfer}
              clearable
            />
            <Select
              label={t('categoryLabel')}
              data={categoryOptions}
              value={categoryId}
              onChange={setCategoryId}
              disabled={!needsCategory}
              clearable
            />
            <TextInput label={t('noteLabel')} value={note} onChange={(event) => setNote(event.currentTarget.value)} />
            <Switch label={t('refundLabel')} checked={isRefund} onChange={(event) => setIsRefund(event.currentTarget.checked)} disabled={Number(type) !== TransactionType.Expense} />
            <Group>
              <Button type="submit" loading={upsertTransaction.isPending}>
                {selectedTransaction ? t('saveTransaction') : t('addTransaction')}
              </Button>
              {selectedTransaction && (
                <Button variant="default" type="button" onClick={resetForm}>
                  {t('cancelEdit')}
                </Button>
              )}
            </Group>
          </form>
        </Card>

        <Card shadow="sm" radius="md" className="surface-card">
          <Text fw={600}>{t('transactionsTitle')}</Text>
          <Text className="section-hint">{t('summaryCurrencyHint')}</Text>
          <Stack gap="xs" mt="sm" className="rows-stack">
            {Object.entries(groupedTransactions).map(([date, items]) => (
              <div key={date} className="ledger-day-group">
                <div className="ledger-day-header">{date}</div>
                {items.map((item) => (
                  <div key={item.id} className="list-row">
                    <div className="list-row-main">
                      <span className="list-row-title">{getTransactionTypeLabel(item.type)}</span>
                      <span className="list-row-meta">
                        {item.currency} {item.amount.toFixed(2)}
                      </span>
                    </div>
                    <Group gap="xs">
                      <Text className={item.type === TransactionType.Income ? 'amount-positive' : 'amount-negative'}>
                        {item.currency} {item.amount.toFixed(2)}
                      </Text>
                      <Button size="xs" variant="light" onClick={() => startEdit(item)}>
                        {t('editButton')}
                      </Button>
                    </Group>
                  </div>
                ))}
              </div>
            ))}
            {!transactionsQuery.data?.length && <Text c="dimmed">{t('noTransactions')}</Text>}
          </Stack>
        </Card>
      </div>
    </section>
  )
}
