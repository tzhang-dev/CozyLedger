import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Card, Group, Loader, Text, Title } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { getCategoryDistribution, getMonthlySummary } from '../lib/cozyApi'
import { CategoryType } from '../lib/types'

type Props = {
  token: string
  bookId: string
}

/**
 * Shows a monthly snapshot and top expense categories for the active book.
 */
export function DashboardPage({ token, bookId }: Props) {
  const { t } = useTranslation()
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

  const topCategories = distributionQuery.data?.items.slice(0, 5) ?? []
  const maxCategoryAmount = Math.max(...topCategories.map((item) => item.totalBaseAmount), 1)

  return (
    <section className="page-panel">
      <div className="page-titlebar">
        <div>
          <Title order={2}>{t('dashboardTitle')}</Title>
          <p>
            {year}-{String(month).padStart(2, '0')}
          </p>
        </div>
      </div>
      {(summaryQuery.isLoading || distributionQuery.isLoading) && <Loader size="sm" />}
      {summaryQuery.data && (
        <Card shadow="sm" radius="md" className="surface-card hero-card">
          <Text size="sm" c="dimmed">
            {t('summaryTitle')}
          </Text>
          <Text className={summaryQuery.data.netTotal < 0 ? 'summary-number summary-number-negative' : 'summary-number'}>
            {summaryQuery.data.baseCurrency} {summaryQuery.data.netTotal.toFixed(2)}
          </Text>
          <div className="metric-chip-row">
            <span className="metric-chip">
              {t('summaryIncome')}: {summaryQuery.data.baseCurrency} {summaryQuery.data.incomeTotal.toFixed(2)}
            </span>
            <span className="metric-chip">
              {t('summaryExpense')}: {summaryQuery.data.baseCurrency} {summaryQuery.data.expenseTotal.toFixed(2)}
            </span>
          </div>
        </Card>
      )}
      {summaryQuery.data && (
        <Group className="tile-grid" grow>
          <Card shadow="sm" radius="md" className="surface-card status-card">
            <Text size="sm" c="dimmed">
              {t('summaryIncome')}
            </Text>
            <Text fw={700} size="xl" c="cyan.3">
              {summaryQuery.data.baseCurrency} {summaryQuery.data.incomeTotal.toFixed(2)}
            </Text>
          </Card>
          <Card shadow="sm" radius="md" className="surface-card status-card">
            <Text size="sm" c="dimmed">
              {t('summaryExpense')}
            </Text>
            <Text fw={700} size="xl" c="red.4">
              {summaryQuery.data.baseCurrency} {summaryQuery.data.expenseTotal.toFixed(2)}
            </Text>
          </Card>
          <Card shadow="sm" radius="md" className="surface-card status-card">
            <Text size="sm" c="dimmed">
              {t('summaryNet')}
            </Text>
            <Text fw={700} size="xl">
              {summaryQuery.data.baseCurrency} {summaryQuery.data.netTotal.toFixed(2)}
            </Text>
          </Card>
        </Group>
      )}

      <Card shadow="sm" radius="md" className="surface-card">
        <Text fw={600}>{t('topExpenseCategoriesMonth')}</Text>
        <Text className="section-hint">{t('categoryDistributionHint')}</Text>
        {topCategories.length ? (
          <div className="rows-stack">
            {topCategories.map((item) => (
              <div key={item.categoryId} className="list-row">
                <div className="list-row-main">
                  <span className="list-row-title">{item.categoryNameEn}</span>
                  <span className="list-row-meta">
                    {((item.totalBaseAmount / maxCategoryAmount) * 100).toFixed(1)}%
                  </span>
                  <div className="bar-track">
                    <div className="bar-fill" style={{ width: `${(item.totalBaseAmount / maxCategoryAmount) * 100}%` }} />
                  </div>
                </div>
                <span className="amount-negative">
                  {distributionQuery.data?.baseCurrency} {item.totalBaseAmount.toFixed(2)}
                </span>
              </div>
            ))}
          </div>
        ) : (
          <Text c="dimmed">{t('noDataYet')}</Text>
        )}
      </Card>
    </section>
  )
}
