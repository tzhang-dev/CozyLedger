import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Card, Group, NumberInput, Select, Stack, Text, Title } from '@mantine/core'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from 'recharts'
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

const categoryColors = ['#D16F2F', '#65A38D', '#50709A', '#CC8B65', '#7B6EA3', '#5A8B4B']

/**
 * Renders chart-based monthly summary and category distribution for the active book.
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

  const summaryChartData = useMemo(() => {
    if (!summaryQuery.data) {
      return []
    }

    return [
      { name: 'Income', value: summaryQuery.data.incomeTotal },
      { name: 'Expense', value: summaryQuery.data.expenseTotal },
      { name: 'Net', value: summaryQuery.data.netTotal }
    ]
  }, [summaryQuery.data])

  const categoryChartData = useMemo(() => {
    const items = distributionQuery.data?.items ?? []
    return items.map((item) => ({
      name: item.categoryNameEn,
      value: item.totalBaseAmount
    }))
  }, [distributionQuery.data])

  const baseCurrency = summaryQuery.data?.baseCurrency ?? distributionQuery.data?.baseCurrency ?? 'N/A'
  const formatCurrencyValue = (value: number | string | undefined) => {
    const numericValue = Number(value ?? 0)
    return `${baseCurrency} ${numericValue.toFixed(2)}`
  }

  return (
    <section className="page-panel">
      <Title order={2}>Reports</Title>
      <Group>
        <NumberInput label="Year" value={year} onChange={(value) => setYear(Number(value || now.getUTCFullYear()))} />
        <NumberInput label="Month" value={month} min={1} max={12} onChange={(value) => setMonth(Number(value || 1))} />
        <Select
          label="Category view"
          data={typeOptions}
          value={distributionType}
          onChange={(value) => setDistributionType(value ?? String(CategoryType.Expense))}
        />
      </Group>

      <Card shadow="sm" radius="md">
        <Text fw={600}>Summary ({baseCurrency})</Text>
        <Text c="dimmed" size="sm">
          Totals are shown in the book base currency.
        </Text>
        <div className="chart-box">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={summaryChartData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="name" />
              <YAxis />
              <Tooltip formatter={formatCurrencyValue} />
              <Legend />
              <Bar dataKey="value" name={`Amount (${baseCurrency})`}>
                {summaryChartData.map((entry, index) => (
                  <Cell key={`${entry.name}-${index}`} fill={categoryColors[index % categoryColors.length]} />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      </Card>

      <Card shadow="sm" radius="md">
        <Text fw={600}>Category Distribution ({baseCurrency})</Text>
        <Text c="dimmed" size="sm">
          Each slice/bar value is expressed in base currency.
        </Text>
        <div className="split-grid report-charts">
          <div className="chart-box">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie dataKey="value" data={categoryChartData} nameKey="name" outerRadius={100} label>
                  {categoryChartData.map((entry, index) => (
                    <Cell key={`${entry.name}-${index}`} fill={categoryColors[index % categoryColors.length]} />
                  ))}
                </Pie>
                <Tooltip formatter={formatCurrencyValue} />
              </PieChart>
            </ResponsiveContainer>
          </div>

          <div className="chart-box">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={categoryChartData} layout="vertical" margin={{ left: 16, right: 16 }}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis type="number" />
                <YAxis dataKey="name" type="category" width={120} />
                <Tooltip formatter={formatCurrencyValue} />
                <Bar dataKey="value" name={`Amount (${baseCurrency})`} fill="#65A38D" />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        {!categoryChartData.length && (
          <Stack gap="xs" mt="sm">
            <Text c="dimmed">No category data for selected month.</Text>
          </Stack>
        )}
      </Card>
    </section>
  )
}
