import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Card, Group, NumberInput, Select, Stack, Text, Title } from '@mantine/core'
import { getCategoryDistribution, getMonthlySummary } from '../lib/cozyApi'
import { CategoryType } from '../lib/types'

type Props = {
  token: string
  bookId: string
}

const typeOptions = [
  { label: 'Expense', value: String(CategoryType.Expense) },
  { label: 'Income', value: String(CategoryType.Income) }
]

/**
 * Renders monthly summary and category distribution reports for the active book.
 */
export function ReportsPage({ token, bookId }: Props) {
  const now = new Date()
  const [year, setYear] = useState(now.getUTCFullYear())
  const [month, setMonth] = useState(now.getUTCMonth() + 1)
  const [distributionType, setDistributionType] = useState(String(CategoryType.Expense))

  const summaryQuery = useQuery({
    queryKey: ['reports-summary', bookId, year, month],
    queryFn: () => getMonthlySummary(token, bookId, year, month)
  })

  const distributionQuery = useQuery({
    queryKey: ['reports-distribution', bookId, year, month, distributionType],
    queryFn: () => getCategoryDistribution(token, bookId, year, month, Number(distributionType) as CategoryType)
  })

  return (
    <section className="page-panel">
      <Title order={2}>Reports</Title>
      <Group>
        <NumberInput label="Year" value={year} onChange={(value) => setYear(Number(value || now.getUTCFullYear()))} />
        <NumberInput label="Month" value={month} min={1} max={12} onChange={(value) => setMonth(Number(value || 1))} />
        <Select label="Category view" data={typeOptions} value={distributionType} onChange={(value) => setDistributionType(value ?? String(CategoryType.Expense))} />
      </Group>

      {summaryQuery.data && (
        <Group className="tile-grid" grow>
          <Card shadow="sm" radius="md" className="status-card">
            <Text size="sm" c="dimmed">
              Income
            </Text>
            <Text fw={700}>
              {summaryQuery.data.baseCurrency} {summaryQuery.data.incomeTotal.toFixed(2)}
            </Text>
          </Card>
          <Card shadow="sm" radius="md" className="status-card">
            <Text size="sm" c="dimmed">
              Expense
            </Text>
            <Text fw={700}>
              {summaryQuery.data.baseCurrency} {summaryQuery.data.expenseTotal.toFixed(2)}
            </Text>
          </Card>
          <Card shadow="sm" radius="md" className="status-card">
            <Text size="sm" c="dimmed">
              Net
            </Text>
            <Text fw={700}>
              {summaryQuery.data.baseCurrency} {summaryQuery.data.netTotal.toFixed(2)}
            </Text>
          </Card>
        </Group>
      )}

      <Card shadow="sm" radius="md">
        <Text fw={600}>Category distribution</Text>
        <Stack gap="xs" mt="sm">
          {distributionQuery.data?.items.map((item) => (
            <Group key={item.categoryId} justify="space-between">
              <Text>{item.categoryNameEn}</Text>
              <Text fw={600}>
                {distributionQuery.data?.baseCurrency} {item.totalBaseAmount.toFixed(2)}
              </Text>
            </Group>
          ))}
          {!distributionQuery.data?.items.length && <Text c="dimmed">No category data for selected month.</Text>}
        </Stack>
      </Card>
    </section>
  )
}
