import { useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Card, Checkbox, Group, Select, Stack, Text, TextInput, Title } from '@mantine/core'
import {
  createAccount,
  createCategory,
  listAccounts,
  listCategories,
  updateAccount,
  updateCategory
} from '../lib/cozyApi'
import { AccountType, CategoryType } from '../lib/types'
import type { AccountResponse, CategoryResponse } from '../lib/types'

type Props = {
  token: string
  bookId: string
}

const accountTypeOptions = [
  { label: 'Cash', value: String(AccountType.Cash) },
  { label: 'Bank', value: String(AccountType.Bank) },
  { label: 'Credit Card', value: String(AccountType.CreditCard) },
  { label: 'Investment', value: String(AccountType.Investment) },
  { label: 'Liability', value: String(AccountType.Liability) },
  { label: 'Other', value: String(AccountType.Other) }
]

const categoryTypeOptions = [
  { label: 'Expense', value: String(CategoryType.Expense) },
  { label: 'Income', value: String(CategoryType.Income) }
]

/**
 * Manages account and category CRUD for the active book.
 */
export function AccountsPage({ token, bookId }: Props) {
  const queryClient = useQueryClient()
  const [accountName, setAccountName] = useState('')
  const [accountCurrency, setAccountCurrency] = useState('USD')
  const [accountType, setAccountType] = useState(String(AccountType.Cash))
  const [selectedAccountId, setSelectedAccountId] = useState<string | null>(null)
  const [categoryName, setCategoryName] = useState('')
  const [categoryType, setCategoryType] = useState(String(CategoryType.Expense))
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(null)

  const accountsQuery = useQuery({
    queryKey: ['accounts', bookId],
    queryFn: () => listAccounts(token, bookId)
  })

  const categoriesQuery = useQuery({
    queryKey: ['categories', bookId],
    queryFn: () => listCategories(token, bookId)
  })

  const selectedAccount = useMemo(
    () => accountsQuery.data?.find((account) => account.id === selectedAccountId),
    [accountsQuery.data, selectedAccountId]
  )

  const selectedCategory = useMemo(
    () => categoriesQuery.data?.find((category) => category.id === selectedCategoryId),
    [categoriesQuery.data, selectedCategoryId]
  )

  const upsertAccount = useMutation({
    mutationFn: async () => {
      const payload = {
        nameEn: accountName,
        nameZhHans: accountName,
        type: Number(accountType) as AccountType,
        currency: accountCurrency,
        isHidden: false,
        includeInNetWorth: true,
        note: null
      }

      if (selectedAccount) {
        return updateAccount(token, bookId, selectedAccount.id, payload)
      }

      return createAccount(token, bookId, payload)
    },
    onSuccess: async () => {
      setAccountName('')
      setSelectedAccountId(null)
      await queryClient.invalidateQueries({ queryKey: ['accounts', bookId] })
    }
  })

  const upsertCategory = useMutation({
    mutationFn: async () => {
      const payload = {
        nameEn: categoryName,
        nameZhHans: categoryName,
        type: Number(categoryType) as CategoryType,
        parentId: null,
        isActive: true
      }

      if (selectedCategory) {
        return updateCategory(token, bookId, selectedCategory.id, payload)
      }

      return createCategory(token, bookId, payload)
    },
    onSuccess: async () => {
      setCategoryName('')
      setSelectedCategoryId(null)
      await queryClient.invalidateQueries({ queryKey: ['categories', bookId] })
    }
  })

  const handleAccountSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!accountName.trim()) {
      return
    }

    upsertAccount.mutate()
  }

  const handleCategorySubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!categoryName.trim()) {
      return
    }

    upsertCategory.mutate()
  }

  const startAccountEdit = (account: AccountResponse) => {
    setSelectedAccountId(account.id)
    setAccountName(account.nameEn)
    setAccountCurrency(account.currency)
    setAccountType(String(account.type))
  }

  const startCategoryEdit = (category: CategoryResponse) => {
    setSelectedCategoryId(category.id)
    setCategoryName(category.nameEn)
    setCategoryType(String(category.type))
  }

  const getCategoryTypeLabel = (value: CategoryType) =>
    value === CategoryType.Income ? 'Income' : 'Expense'

  return (
    <section className="page-panel">
      <Title order={2}>Accounts & Categories</Title>

      <div className="split-grid">
        <Card shadow="sm" radius="md">
          <form onSubmit={handleAccountSubmit} className="form-grid">
            <Text fw={600}>{selectedAccount ? 'Edit account' : 'Create account'}</Text>
            <TextInput value={accountName} onChange={(e) => setAccountName(e.currentTarget.value)} label="Name" required />
            <TextInput
              value={accountCurrency}
              onChange={(e) => setAccountCurrency(e.currentTarget.value.toUpperCase())}
              label="Currency"
              maxLength={3}
              required
            />
            <Select value={accountType} onChange={(value) => setAccountType(value ?? String(AccountType.Cash))} data={accountTypeOptions} label="Type" />
            <Button type="submit" loading={upsertAccount.isPending}>
              {selectedAccount ? 'Save account' : 'Add account'}
            </Button>
          </form>
          <Stack gap="xs" mt="md">
            {accountsQuery.data?.map((account) => (
              <Group key={account.id} justify="space-between">
                <Text>
                  {account.nameEn} ({account.currency})
                </Text>
                <Button variant="light" size="xs" onClick={() => startAccountEdit(account)}>
                  Edit
                </Button>
              </Group>
            ))}
          </Stack>
        </Card>

        <Card shadow="sm" radius="md">
          <form onSubmit={handleCategorySubmit} className="form-grid">
            <Text fw={600}>{selectedCategory ? 'Edit category' : 'Create category'}</Text>
            <TextInput value={categoryName} onChange={(e) => setCategoryName(e.currentTarget.value)} label="Name" required />
            <Select
              value={categoryType}
              onChange={(value) => setCategoryType(value ?? String(CategoryType.Expense))}
              data={categoryTypeOptions}
              label="Type"
            />
            <Checkbox label="Active" checked readOnly />
            <Button type="submit" loading={upsertCategory.isPending}>
              {selectedCategory ? 'Save category' : 'Add category'}
            </Button>
          </form>
          <Stack gap="xs" mt="md">
            {categoriesQuery.data?.map((category) => (
              <Group key={category.id} justify="space-between">
                <Text>
                  {category.nameEn} ({getCategoryTypeLabel(category.type)})
                </Text>
                <Button variant="light" size="xs" onClick={() => startCategoryEdit(category)}>
                  Edit
                </Button>
              </Group>
            ))}
          </Stack>
        </Card>
      </div>
    </section>
  )
}
