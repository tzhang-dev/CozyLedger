import { useCallback, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ActionIcon, Alert, Button, Group, Menu, Select, Text, TextInput } from '@mantine/core'
import { IconDots } from '@tabler/icons-react'
import { useTranslation } from 'react-i18next'
import { ActionFormModal } from '../components/ActionFormModal'
import { CurrencySelect } from '../components/CurrencySelect'
import { EntityDetailsModal } from '../components/EntityDetailsModal'
import {
  createAccount,
  deleteAccount,
  listAccounts,
  listBooks,
  listTransactions,
  updateAccount
} from '../lib/cozyApi'
import { AccountType, TransactionType } from '../lib/types'

type Props = {
  token: string
  bookId: string
}

type AccountFormMode = 'create' | 'edit'

/**
 * Manages account and category CRUD for the active book.
 */
export function AccountsPage({ token, bookId }: Props) {
  const { t, i18n } = useTranslation()
  const queryClient = useQueryClient()
  const [selectedAccountId, setSelectedAccountId] = useState<string | null>(null)
  const [accountFormMode, setAccountFormMode] = useState<AccountFormMode | null>(null)
  const [accountName, setAccountName] = useState('')
  const [accountCurrency, setAccountCurrency] = useState('USD')
  const [accountType, setAccountType] = useState(String(AccountType.Cash))
  const [detailsOpen, setDetailsOpen] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  const accountTypeOptions = [
    { label: t('typeCash'), value: String(AccountType.Cash) },
    { label: t('typeBank'), value: String(AccountType.Bank) },
    { label: t('typeCreditCard'), value: String(AccountType.CreditCard) },
    { label: t('typeInvestment'), value: String(AccountType.Investment) },
    { label: t('typeLiability'), value: String(AccountType.Liability) },
    { label: t('typeOther'), value: String(AccountType.Other) }
  ]

  const accountsQuery = useQuery({
    queryKey: ['accounts', bookId],
    queryFn: () => listAccounts(token, bookId)
  })

  const booksQuery = useQuery({
    queryKey: ['books', token],
    queryFn: () => listBooks(token)
  })

  const transactionsQuery = useQuery({
    queryKey: ['transactions', bookId],
    queryFn: () => listTransactions(token, bookId)
  })
  const bookBaseCurrency = booksQuery.data?.find((book) => book.id === bookId)?.baseCurrency.toUpperCase() ?? 'USD'

  const balanceByAccount = useMemo(() => {
    const balances = new Map<string, number>()
    for (const transaction of transactionsQuery.data ?? []) {
      if (transaction.type === TransactionType.Transfer) {
        balances.set(transaction.accountId, (balances.get(transaction.accountId) ?? 0) - transaction.amount)
        if (transaction.toAccountId) {
          balances.set(transaction.toAccountId, (balances.get(transaction.toAccountId) ?? 0) + transaction.amount)
        }
        continue
      }

      if (transaction.type === TransactionType.Expense) {
        balances.set(transaction.accountId, (balances.get(transaction.accountId) ?? 0) - transaction.amount)
        continue
      }

      balances.set(transaction.accountId, (balances.get(transaction.accountId) ?? 0) + transaction.amount)
    }

    return balances
  }, [transactionsQuery.data])

  const getAccountTypeLabel = useCallback((value: AccountType) => {
    switch (value) {
      case AccountType.Cash:
        return t('typeCash')
      case AccountType.Bank:
        return t('typeBank')
      case AccountType.CreditCard:
        return t('typeCreditCard')
      case AccountType.Investment:
        return t('typeInvestment')
      case AccountType.Liability:
        return t('typeLiability')
      default:
        return t('typeOther')
    }
  }, [t])

  const selectedAccount = useMemo(() => {
    const accounts = accountsQuery.data ?? []
    return accounts.find((account) => account.id === selectedAccountId) ?? accounts[0] ?? null
  }, [accountsQuery.data, selectedAccountId])

  const groupedAccounts = useMemo(() => {
    const accounts = accountsQuery.data ?? []
    const typeOrder: AccountType[] = [
      AccountType.Cash,
      AccountType.Bank,
      AccountType.CreditCard,
      AccountType.Investment,
      AccountType.Liability,
      AccountType.Other
    ]

    return typeOrder
      .map((type) => {
        const items = accounts
          .filter((account) => account.type === type)
          .sort((a, b) =>
            (i18n.language === 'zh' ? a.nameZhHans : a.nameEn).localeCompare(i18n.language === 'zh' ? b.nameZhHans : b.nameEn)
          )
        return {
          type,
          label: getAccountTypeLabel(type),
          items
        }
      })
      .filter((group) => group.items.length > 0)
  }, [accountsQuery.data, getAccountTypeLabel, i18n.language])

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

      if (accountFormMode === 'edit' && selectedAccount) {
        return updateAccount(token, bookId, selectedAccount.id, payload)
      }

      return createAccount(token, bookId, payload)
    },
    onSuccess: async (account) => {
      setDeleteError(null)
      setSelectedAccountId(account.id)
      setAccountFormMode(null)
      setAccountName('')
      await queryClient.invalidateQueries({ queryKey: ['accounts', bookId] })
      await queryClient.invalidateQueries({ queryKey: ['transactions', bookId] })
    }
  })

  const deleteAccountMutation = useMutation({
    mutationFn: async (accountId: string) => deleteAccount(token, bookId, accountId),
    onSuccess: async (_, deletedAccountId) => {
      setDeleteError(null)
      setAccountFormMode(null)
      if (selectedAccountId === deletedAccountId) {
        setSelectedAccountId(null)
      }

      await queryClient.invalidateQueries({ queryKey: ['accounts', bookId] })
      await queryClient.invalidateQueries({ queryKey: ['transactions', bookId] })
    },
    onError: () => {
      setDeleteError(t('deleteAccountBlocked'))
    }
  })

  const handleAccountSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!accountName.trim()) {
      return
    }

    upsertAccount.mutate()
  }

  const openCreateAccount = () => {
    setDeleteError(null)
    setAccountFormMode('create')
    setAccountName('')
    setAccountCurrency(bookBaseCurrency)
    setAccountType(String(AccountType.Cash))
  }

  const openEditAccount = () => {
    if (!selectedAccount) {
      return
    }

    setDeleteError(null)
    setAccountFormMode('edit')
    setAccountName(i18n.language === 'zh' ? selectedAccount.nameZhHans : selectedAccount.nameEn)
    setAccountCurrency(selectedAccount.currency)
    setAccountType(String(selectedAccount.type))
  }

  const requestDeleteAccount = () => {
    if (!selectedAccount) {
      return
    }

    const confirmed = window.confirm(t('deleteAccountConfirm', { name: i18n.language === 'zh' ? selectedAccount.nameZhHans : selectedAccount.nameEn }))
    if (!confirmed) {
      return
    }

    deleteAccountMutation.mutate(selectedAccount.id)
  }

  const selectedAccountBalance = selectedAccount ? balanceByAccount.get(selectedAccount.id) ?? 0 : 0

  return (
    <section className="cl-page">
      <header className="cl-header">
        <div className="cl-header-inner">
          <h1 className="cl-header-title">{t('accountsTitle')}</h1>
          <p className="cl-header-subtitle">{t('accountsManageSubtitle')}</p>
          <Menu shadow="md" width={220} position="bottom-end">
            <Menu.Target>
              <ActionIcon variant="light" size="lg" aria-label={t('accountActionsMenu')} style={{ justifySelf: 'end' }}>
                <IconDots size={18} />
              </ActionIcon>
            </Menu.Target>
            <Menu.Dropdown>
              <Menu.Item onClick={openCreateAccount}>{t('addAccount')}</Menu.Item>
              <Menu.Item onClick={openEditAccount} disabled={!selectedAccount}>
                {t('editAccount')}
              </Menu.Item>
              <Menu.Item color="red" onClick={requestDeleteAccount} disabled={!selectedAccount || deleteAccountMutation.isPending}>
                {t('deleteAccount')}
              </Menu.Item>
            </Menu.Dropdown>
          </Menu>
        </div>
      </header>

      <div className="cl-body">
        {deleteError ? <Alert color="red">{deleteError}</Alert> : null}

        <div className="cl-card">
          <div className="cl-list">
            {groupedAccounts.map((group) => (
              <div key={String(group.type)} className="cl-list-group">
                <p className="cl-list-group-label">{group.label}</p>
                {group.items.map((account) => (
                  <button
                    key={account.id}
                    type="button"
                    className="cl-list-row"
                    onClick={() => {
                      setSelectedAccountId(account.id)
                      setDetailsOpen(true)
                    }}
                  >
                    <span className="cl-list-row-main">
                      <span className="cl-list-row-title">{i18n.language === 'zh' ? account.nameZhHans : account.nameEn}</span>
                      <span className="cl-list-row-meta">{getAccountTypeLabel(account.type)}</span>
                    </span>
                    <Text className={Number((balanceByAccount.get(account.id) ?? 0).toFixed(2)) >= 0 ? 'cl-amount-income' : 'cl-amount-expense'}>
                      {account.currency} {(balanceByAccount.get(account.id) ?? 0).toFixed(2)}
                    </Text>
                  </button>
                ))}
              </div>
            ))}
            {!accountsQuery.data?.length ? <p className="cl-empty">{t('noAccounts')}</p> : null}
          </div>
        </div>
      </div>

      <ActionFormModal
        opened={Boolean(accountFormMode)}
        title={accountFormMode === 'edit' ? t('editAccount') : t('createAccount')}
        onClose={() => setAccountFormMode(null)}
      >
        <form onSubmit={handleAccountSubmit} className="cl-form-grid">
          <TextInput value={accountName} onChange={(event) => setAccountName(event.currentTarget.value)} label={t('nameLabel')} required />
          <CurrencySelect
            label={t('currencyLabel')}
            value={accountCurrency}
            onChange={setAccountCurrency}
            baseCurrency={bookBaseCurrency}
            knownCurrencies={(accountsQuery.data ?? []).map((item) => item.currency)}
            required
          />
          <Select value={accountType} onChange={(value) => setAccountType(value ?? String(AccountType.Cash))} data={accountTypeOptions} label={t('typeLabel')} />
          <Group>
            <Button type="submit" loading={upsertAccount.isPending}>
              {accountFormMode === 'edit' ? t('saveAccount') : t('addAccount')}
            </Button>
            <Button type="button" variant="default" onClick={() => setAccountFormMode(null)}>
              {t('cancelEdit')}
            </Button>
          </Group>
        </form>
      </ActionFormModal>

      <EntityDetailsModal
        opened={detailsOpen}
        title={(selectedAccount ? (i18n.language === 'zh' ? selectedAccount.nameZhHans : selectedAccount.nameEn) : null) ?? t('accountDetailsTitle')}
        onClose={() => setDetailsOpen(false)}
      >
        {selectedAccount ? (
          <div className="cl-form-grid">
            <div className="cl-list-row">
              <span className="cl-list-row-title">{t('nameLabel')}</span>
              <span className="cl-list-row-meta">{i18n.language === 'zh' ? selectedAccount.nameZhHans : selectedAccount.nameEn}</span>
            </div>
            <div className="cl-list-row">
              <span className="cl-list-row-title">{t('typeLabel')}</span>
              <span className="cl-list-row-meta">{getAccountTypeLabel(selectedAccount.type)}</span>
            </div>
            <div className="cl-list-row">
              <span className="cl-list-row-title">{t('currencyLabel')}</span>
              <span className="cl-list-row-meta">{selectedAccount.currency}</span>
            </div>
            <div className="cl-list-row">
              <span className="cl-list-row-title">{t('accountBalanceLabel')}</span>
              <Text className={Number(selectedAccountBalance.toFixed(2)) >= 0 ? 'cl-amount-income' : 'cl-amount-expense'}>
                {selectedAccount.currency} {selectedAccountBalance.toFixed(2)}
              </Text>
            </div>
          </div>
        ) : (
          <p className="cl-empty">{t('accountNoSelection')}</p>
        )}
      </EntityDetailsModal>
    </section>
  )
}
