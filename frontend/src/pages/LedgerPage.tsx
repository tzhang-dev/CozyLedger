import { useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Card, Group, NumberInput, Select, Stack, Switch, Text, TextInput, Title } from '@mantine/core'
import { createTransaction, listAccounts, listCategories, listTransactions, updateTransaction } from '../lib/cozyApi'
import { TransactionType } from '../lib/types'
import type { TransactionResponse } from '../lib/types'

type Props = {
  token: string
  bookId: string
}

const transactionTypeOptions = [
  { label: 'Expense', value: String(TransactionType.Expense) },
  { label: 'Income', value: String(TransactionType.Income) },
  { label: 'Transfer', value: String(TransactionType.Transfer) },
  { label: 'Balance Adjustment', value: String(TransactionType.BalanceAdjustment) }
]

/**
 * Manages transaction create/update flows and renders the ledger list.
 */
export function LedgerPage({ token, bookId }: Props) {
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
        return 'Expense'
      case TransactionType.Income:
        return 'Income'
      case TransactionType.Transfer:
        return 'Transfer'
      case TransactionType.BalanceAdjustment:
        return 'Balance Adjustment'
      case TransactionType.LiabilityAdjustment:
        return 'Liability Adjustment'
      default:
        return 'Unknown'
    }
  }

  return (
    <section className="page-panel">
      <Title order={2}>Ledger</Title>
      <div className="split-grid">
        <Card shadow="sm" radius="md">
          <form onSubmit={handleSubmit} className="form-grid">
            <Text fw={600}>{selectedTransaction ? 'Edit transaction' : 'Create transaction'}</Text>
            <Select label="Type" data={transactionTypeOptions} value={type} onChange={(value) => setType(value ?? String(TransactionType.Expense))} />
            <TextInput label="Date (UTC)" value={dateUtc} onChange={(event) => setDateUtc(event.currentTarget.value)} required />
            <NumberInput label="Amount" value={amount} onChange={(value) => setAmount(Number(value ?? 0))} required />
            <TextInput label="Currency" value={currency} onChange={(event) => setCurrency(event.currentTarget.value.toUpperCase())} maxLength={3} required />
            <Select label="Account" data={accountOptions} value={accountId} onChange={setAccountId} searchable required />
            <Select
              label="Destination account"
              data={accountOptions}
              value={toAccountId}
              onChange={setToAccountId}
              disabled={!isTransfer}
              clearable
            />
            <Select
              label="Category"
              data={categoryOptions}
              value={categoryId}
              onChange={setCategoryId}
              disabled={!needsCategory}
              clearable
            />
            <TextInput label="Note" value={note} onChange={(event) => setNote(event.currentTarget.value)} />
            <Switch label="Refund" checked={isRefund} onChange={(event) => setIsRefund(event.currentTarget.checked)} disabled={Number(type) !== TransactionType.Expense} />
            <Group>
              <Button type="submit" loading={upsertTransaction.isPending}>
                {selectedTransaction ? 'Save transaction' : 'Add transaction'}
              </Button>
              {selectedTransaction && (
                <Button variant="default" type="button" onClick={resetForm}>
                  Cancel edit
                </Button>
              )}
            </Group>
          </form>
        </Card>

        <Card shadow="sm" radius="md">
          <Text fw={600}>Transactions</Text>
          <Stack gap="xs" mt="sm">
            {transactionsQuery.data?.map((item) => (
              <Group key={item.id} justify="space-between">
                <Text>
                  {item.dateUtc.slice(0, 10)} | {getTransactionTypeLabel(item.type)} | {item.currency} {item.amount}
                </Text>
                <Button size="xs" variant="light" onClick={() => startEdit(item)}>
                  Edit
                </Button>
              </Group>
            ))}
            {!transactionsQuery.data?.length && <Text c="dimmed">No transactions yet.</Text>}
          </Stack>
        </Card>
      </div>
    </section>
  )
}
