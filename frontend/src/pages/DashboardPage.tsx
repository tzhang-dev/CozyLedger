import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Card, Group, Loader, Text, Title } from '@mantine/core'
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

  return (
    <section className="page-panel">
      <Title order={2}>Household Snapshot</Title>
      {(summaryQuery.isLoading || distributionQuery.isLoading) && <Loader size="sm" />}
      {summaryQuery.data && (
        <Group className="tile-grid" grow>
          <Card shadow="sm" radius="md" className="status-card">
            <Text size="sm" c="dimmed">
              Income
            </Text>
            <Text fw={700} size="xl">
              {summaryQuery.data.baseCurrency} {summaryQuery.data.incomeTotal.toFixed(2)}
            </Text>
          </Card>
          <Card shadow="sm" radius="md" className="status-card">
            <Text size="sm" c="dimmed">
              Expense
            </Text>
            <Text fw={700} size="xl">
              {summaryQuery.data.baseCurrency} {summaryQuery.data.expenseTotal.toFixed(2)}
            </Text>
          </Card>
          <Card shadow="sm" radius="md" className="status-card">
            <Text size="sm" c="dimmed">
              Net
            </Text>
            <Text fw={700} size="xl">
              {summaryQuery.data.baseCurrency} {summaryQuery.data.netTotal.toFixed(2)}
            </Text>
          </Card>
        </Group>
      )}

      <Card shadow="sm" radius="md">
        <Text fw={600}>Top Expense Categories (This Month)</Text>
        {distributionQuery.data?.items.length ? (
          distributionQuery.data.items.slice(0, 5).map((item) => (
            <Group key={item.categoryId} justify="space-between">
              <Text>{item.categoryNameEn}</Text>
              <Text fw={600}>
                {distributionQuery.data.baseCurrency} {item.totalBaseAmount.toFixed(2)}
              </Text>
            </Group>
          ))
        ) : (
          <Text c="dimmed">No data yet.</Text>
        )}
      </Card>
    </section>
  )
}
