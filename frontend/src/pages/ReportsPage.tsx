import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  Bar,
  BarChart,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from 'recharts'
import { IconChevronLeft, IconChevronRight } from '@tabler/icons-react'
import { useTranslation } from 'react-i18next'
import { getCategoryDistribution, getMonthlySummary, listBooks } from '../lib/cozyApi'
import { CategoryType } from '../lib/types'

type Props = {
  token: string
  bookId: string
}

const categoryColors = ['#6d46f1', '#2dd4bf', '#f97316', '#ef4444', '#06b6d4', '#eab308']

/**
 * Renders chart-based monthly summary and category distribution for the active book.
 */
export function ReportsPage({ token, bookId }: Props) {
  const { t, i18n } = useTranslation()
  const now = new Date()
  const [cursor, setCursor] = useState(new Date(now.getUTCFullYear(), now.getUTCMonth(), 1))
  const [distributionType, setDistributionType] = useState(String(CategoryType.Expense))
  const [activeTab, setActiveTab] = useState<'overview' | 'categories'>('overview')

  const year = cursor.getUTCFullYear()
  const month = cursor.getUTCMonth() + 1

  const summaryQuery = useQuery({
    queryKey: ['reports-summary', bookId, year, month],
    queryFn: () => getMonthlySummary(token, bookId, year, month)
  })

  const distributionQuery = useQuery({
    queryKey: ['reports-distribution', bookId, year, month, distributionType],
    queryFn: () => getCategoryDistribution(token, bookId, year, month, Number(distributionType) as CategoryType)
  })
  const booksQuery = useQuery({
    queryKey: ['books', token],
    queryFn: () => listBooks(token)
  })

  const summaryData = useMemo(() => {
    if (!summaryQuery.data) {
      return []
    }

    return [
      { label: t('summaryIncome'), value: summaryQuery.data.incomeTotal, color: '#22c55e' },
      { label: t('summaryExpense'), value: summaryQuery.data.expenseTotal, color: '#ef4444' },
      { label: t('reportsSaved'), value: summaryQuery.data.netTotal, color: '#6d46f1' }
    ]
  }, [summaryQuery.data, t])

  const categoryData = useMemo(
    () =>
      (distributionQuery.data?.items ?? []).map((item, index) => ({
        name: i18n.language === 'zh' ? item.categoryNameZhHans : item.categoryNameEn,
        value: item.totalBaseAmount,
        color: categoryColors[index % categoryColors.length]
      })),
    [distributionQuery.data, i18n.language]
  )

  const bookBaseCurrency = booksQuery.data?.find((book) => book.id === bookId)?.baseCurrency.toUpperCase()
  const baseCurrency = summaryQuery.data?.baseCurrency ?? distributionQuery.data?.baseCurrency ?? bookBaseCurrency ?? ''
  const monthLabel = cursor.toLocaleDateString(i18n.language === 'zh' ? 'zh-CN' : 'en-US', { month: 'long', year: 'numeric' })
  const maxCategoryValue = Math.max(...categoryData.map((item) => item.value), 1)

  return (
    <section className="cl-page">
      <header className="cl-header">
        <div className="cl-header-inner">
          <h1 className="cl-header-title">{t('reportsTitle')}</h1>
          <div className="cl-month-nav">
            <button type="button" className="cl-month-button" onClick={() => setCursor(new Date(year, month - 2, 1))}>
              <IconChevronLeft size={18} />
            </button>
            <span className="cl-month-label">{monthLabel}</span>
            <button type="button" className="cl-month-button" onClick={() => setCursor(new Date(year, month, 1))}>
              <IconChevronRight size={18} />
            </button>
          </div>
          <div className="cl-stat-grid-3">
            {summaryData.map((item) => (
              <div key={item.label} className="cl-stat-box cl-stat-box-net">
                <span className="cl-stat-label">{item.label}</span>
                <span className="cl-stat-value" style={{ color: item.color }}>
                  {baseCurrency} {item.value.toFixed(2)}
                </span>
              </div>
            ))}
          </div>
        </div>
      </header>

      <div className="cl-body">
        <div className="cl-filter-row">
          <button type="button" className={`cl-chip ${activeTab === 'overview' ? 'cl-chip-active' : ''}`} onClick={() => setActiveTab('overview')}>
            {t('reportsTabOverview')}
          </button>
          <button type="button" className={`cl-chip ${activeTab === 'categories' ? 'cl-chip-active' : ''}`} onClick={() => setActiveTab('categories')}>
            {t('reportsTabCategories')}
          </button>
          <button
            type="button"
            className={`cl-chip ${distributionType === String(CategoryType.Expense) ? 'cl-chip-active' : ''}`}
            onClick={() => setDistributionType(String(CategoryType.Expense))}
          >
            {t('typeExpense')}
          </button>
          <button
            type="button"
            className={`cl-chip ${distributionType === String(CategoryType.Income) ? 'cl-chip-active' : ''}`}
            onClick={() => setDistributionType(String(CategoryType.Income))}
          >
            {t('typeIncome')}
          </button>
        </div>

        {activeTab === 'overview' ? (
          <div className="cl-card">
            <p className="cl-card-title">{t('reportsIncomeVsExpense')}</p>
            <p className="cl-card-subtitle">{t('reportsMonthlyTotalsIn', { currency: baseCurrency })}</p>
            <div style={{ width: '100%', height: 280 }}>
              <ResponsiveContainer>
                <BarChart data={summaryData}>
                  <XAxis dataKey="label" tick={{ fill: '#9ca3af', fontSize: 11 }} axisLine={false} tickLine={false} />
                  <YAxis tick={{ fill: '#9ca3af', fontSize: 11 }} axisLine={false} tickLine={false} />
                  <Tooltip formatter={(value) => `${baseCurrency} ${Number(value ?? 0).toFixed(2)}`} />
                  <Bar dataKey="value" radius={[8, 8, 0, 0]}>
                    {summaryData.map((item, index) => (
                      <Cell key={`${item.label}-${index}`} fill={item.color} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>
        ) : null}

        {activeTab === 'categories' ? (
          <>
            <div className="cl-card">
              <p className="cl-card-title">{t('categoryDistributionTitle')}</p>
              <div style={{ width: '100%', height: 260 }}>
                <ResponsiveContainer>
                  <PieChart>
                    <Pie data={categoryData} dataKey="value" nameKey="name" innerRadius={48} outerRadius={88}>
                      {categoryData.map((item, index) => (
                        <Cell key={`${item.name}-${index}`} fill={item.color} />
                      ))}
                    </Pie>
                    <Tooltip formatter={(value) => `${baseCurrency} ${Number(value ?? 0).toFixed(2)}`} />
                  </PieChart>
                </ResponsiveContainer>
              </div>
            </div>

            <div className="cl-card">
              <p className="cl-card-title">{t('reportsBreakdownTitle')}</p>
              <div className="cl-list" style={{ marginTop: 10 }}>
                {categoryData.map((item) => {
                  const width = (item.value / maxCategoryValue) * 100
                  return (
                    <div key={item.name} className="cl-list-row">
                      <div className="cl-list-row-main">
                        <span className="cl-list-row-title">{item.name}</span>
                        <span className="cl-list-row-meta">{width.toFixed(1)}%</span>
                        <div className="cl-progress-track">
                          <div className="cl-progress-fill" style={{ width: `${width}%`, background: item.color }} />
                        </div>
                      </div>
                      <span className="cl-stat-value">
                        {baseCurrency} {item.value.toFixed(2)}
                      </span>
                    </div>
                  )
                })}
                {!categoryData.length ? <p className="cl-empty">{t('noCategoryData')}</p> : null}
              </div>
            </div>
          </>
        ) : null}
      </div>
    </section>
  )
}
