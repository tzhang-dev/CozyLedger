import { useMemo } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  IconArrowDownRight,
  IconArrowUpRight,
  IconBellRinging
} from '@tabler/icons-react'
import { useTranslation } from 'react-i18next'
import { getCategoryDistribution, getMonthlySummary, listBooks, listTransactions } from '../lib/cozyApi'
import { CategoryType, TransactionType } from '../lib/types'

type Props = {
  token: string
  bookId: string
}

/**
 * Shows a monthly snapshot and top expense categories for the active book.
 */
export function DashboardPage({ token, bookId }: Props) {
  const { t, i18n } = useTranslation()
  const now = useMemo(() => new Date(), [])
  const year = now.getUTCFullYear()
  const month = now.getUTCMonth() + 1

  const summaryQuery = useQuery({
    queryKey: ['summary', bookId, year, month],
    queryFn: () => getMonthlySummary(token, bookId, year, month)
  })

  const distributionQuery = useQuery({
    queryKey: ['distribution', bookId, year, month],
    queryFn: () => getCategoryDistribution(token, bookId, year, month, CategoryType.Expense)
  })

  const transactionsQuery = useQuery({
    queryKey: ['transactions', bookId],
    queryFn: () => listTransactions(token, bookId)
  })
  const booksQuery = useQuery({
    queryKey: ['books', token],
    queryFn: () => listBooks(token)
  })

  const topCategories = distributionQuery.data?.items.slice(0, 4) ?? []
  const maxCategoryAmount = Math.max(...topCategories.map((item) => item.totalBaseAmount), 1)
  const recentTransactions = useMemo(
    () => [...(transactionsQuery.data ?? [])].sort((a, b) => b.dateUtc.localeCompare(a.dateUtc)).slice(0, 5),
    [transactionsQuery.data]
  )

  const bookBaseCurrency = booksQuery.data?.find((book) => book.id === bookId)?.baseCurrency.toUpperCase()
  const baseCurrency = summaryQuery.data?.baseCurrency ?? distributionQuery.data?.baseCurrency ?? bookBaseCurrency ?? ''
  const monthLabel = now.toLocaleDateString(i18n.language === 'zh' ? 'zh-CN' : 'en-US', { month: 'long', year: 'numeric' })

  return (
    <section className="cl-page cl-home-page">
      <header className="cl-header cl-home-header">
        <div className="cl-header-inner cl-home-header-inner">
          <div className="cl-home-top-row">
            <div>
              <p className="cl-header-subtitle">{t('homeGreeting')}</p>
              <h1 className="cl-home-name">{t('homeUserName')} <span aria-hidden>👋</span></h1>
            </div>
            <button type="button" className="cl-home-bell" aria-label={t('homeNotificationAria')}>
              <IconBellRinging size={18} />
              <span className="cl-home-bell-dot" aria-hidden />
            </button>
          </div>
          <div className="cl-home-balance-block">
            <p className="cl-home-balance-label">{t('homeTotalBalance')}</p>
            <h2 className="cl-home-balance-value">
              {baseCurrency} {(summaryQuery.data?.netTotal ?? 0).toFixed(2)}
            </h2>
            <p className="cl-home-balance-month">{monthLabel}</p>
          </div>
        </div>
      </header>

      <div className="cl-body cl-home-body">
        <div className="cl-card cl-home-floating-card">
          <div className="cl-home-mini-grid">
            <div className="cl-home-mini-card cl-home-mini-income">
              <span className="cl-home-mini-head">
                <span className="cl-home-mini-icon"><IconArrowUpRight size={13} /></span>
                <span className="cl-stat-label">{t('summaryIncome')}</span>
              </span>
              <span className="cl-stat-value cl-amount-income">
                {baseCurrency} {(summaryQuery.data?.incomeTotal ?? 0).toFixed(2)}
              </span>
              <span className="cl-list-row-meta">{t('homeThisMonth')}</span>
            </div>
            <div className="cl-home-mini-card cl-home-mini-expense">
              <span className="cl-home-mini-head">
                <span className="cl-home-mini-icon"><IconArrowDownRight size={13} /></span>
                <span className="cl-stat-label">{t('summaryExpense')}</span>
              </span>
              <span className="cl-stat-value cl-amount-expense">
                {baseCurrency} {(summaryQuery.data?.expenseTotal ?? 0).toFixed(2)}
              </span>
              <span className="cl-list-row-meta">{t('homeThisMonth')}</span>
            </div>
          </div>
        </div>

        <div className="cl-action-grid">
          <Link to="/ledger/new" className="cl-action-btn cl-action-btn-income">
            <span aria-hidden>💰</span> {t('homeAddIncome')}
          </Link>
          <Link to="/ledger/new" className="cl-action-btn cl-action-btn-expense">
            <span aria-hidden>💸</span> {t('homeAddExpense')}
          </Link>
          <Link to="/reports" className="cl-action-btn cl-action-btn-reports">
            <span aria-hidden>📊</span> {t('homeActionReports')}
          </Link>
        </div>

        <div className="cl-card">
          <p className="cl-card-title">{t('homeTopExpenseCategories')}</p>
          <p className="cl-card-subtitle">{t('homeCurrentMonthDistribution')}</p>
          <div className="cl-list">
            {topCategories.map((item) => {
              const width = (item.totalBaseAmount / maxCategoryAmount) * 100
              return (
                <div key={item.categoryId} className="cl-list-row">
                  <div className="cl-list-row-main">
                    <span className="cl-list-row-title">{i18n.language === 'zh' ? item.categoryNameZhHans : item.categoryNameEn}</span>
                    <span className="cl-list-row-meta">{width.toFixed(1)}%</span>
                    <div className="cl-progress-track">
                      <div className="cl-progress-fill" style={{ width: `${width}%` }} />
                    </div>
                  </div>
                  <span className="cl-amount-expense">
                    {distributionQuery.data?.baseCurrency ?? baseCurrency} {item.totalBaseAmount.toFixed(2)}
                  </span>
                </div>
              )
            })}
            {!topCategories.length ? <p className="cl-empty">{t('noCategoryData')}</p> : null}
          </div>
        </div>

        <div className="cl-card">
          <p className="cl-card-title">{t('homeRecentTransactions')}</p>
          <div className="cl-list">
            {recentTransactions.map((item) => (
              <div key={item.id} className="cl-list-row">
                <div className="cl-list-row-main">
                  <span className="cl-list-row-title">{item.note || t('homeFallbackTransaction')}</span>
                  <span className="cl-list-row-meta">{item.dateUtc.slice(0, 10)}</span>
                </div>
                <span className={item.type === TransactionType.Income ? 'cl-amount-income' : 'cl-amount-expense'}>
                  {item.currency} {item.amount.toFixed(2)}
                </span>
              </div>
            ))}
            {!recentTransactions.length ? <p className="cl-empty">{t('noTransactions')}</p> : null}
          </div>
        </div>
      </div>
    </section>
  )
}
