import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useViewportSize } from '@mantine/hooks'
import { Bar, BarChart, CartesianGrid, Cell, Pie, PieChart, Tooltip, XAxis, YAxis } from 'recharts'
import { Card, Group, NumberInput, Select, Stack, Text, Title } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { getCategoryDistribution, getMonthlySummary } from '../lib/cozyApi'
import { CategoryType } from '../lib/types'

type Props = {
  token: string
  bookId: string
}

const categoryColors = ['#D16F2F', '#65A38D', '#50709A', '#CC8B65', '#7B6EA3', '#5A8B4B']
const darkChartLabel = '#9aa3b7'
const darkChartGrid = 'rgba(255,255,255,0.12)'
const tooltipStyle = {
  backgroundColor: '#161d2d',
  border: '1px solid rgba(255,255,255,0.12)',
  borderRadius: '12px',
  color: '#f2f5fe'
}

/**
 * Renders chart-based monthly summary and category distribution for the active book.
 */
export function ReportsPage({ token, bookId }: Props) {
  const { t } = useTranslation()
  const { width: viewportWidth } = useViewportSize()
  const now = new Date()
  const [year, setYear] = useState(now.getUTCFullYear())
  const [month, setMonth] = useState(now.getUTCMonth() + 1)
  const [distributionType, setDistributionType] = useState(String(CategoryType.Expense))

  const typeOptions = [
    { label: t('typeExpense'), value: String(CategoryType.Expense) },
    { label: t('typeIncome'), value: String(CategoryType.Income) }
  ]

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
      { name: t('summaryIncome'), value: summaryQuery.data.incomeTotal },
      { name: t('summaryExpense'), value: summaryQuery.data.expenseTotal },
      { name: t('summaryNet'), value: summaryQuery.data.netTotal }
    ]
  }, [summaryQuery.data, t])

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

  const summaryChartWidth = Math.max(320, Math.min(980, Math.floor(viewportWidth - 120)))
  const splitChartWidth =
    viewportWidth > 860
      ? Math.max(280, Math.floor((viewportWidth - 190) / 2))
      : Math.max(280, Math.floor(viewportWidth - 120))
  const chartHeight = 320
  const topDistribution = useMemo(() => categoryChartData.slice().sort((a, b) => b.value - a.value).slice(0, 5), [categoryChartData])
  const maxDistribution = Math.max(...topDistribution.map((item) => item.value), 1)

  return (
    <section className="page-panel">
      <div className="page-titlebar">
        <div>
          <Title order={2}>{t('reportsTitle')}</Title>
          <p>
            {year}-{String(month).padStart(2, '0')}
          </p>
        </div>
      </div>
      <Group>
        <NumberInput label={t('yearLabel')} value={year} onChange={(value) => setYear(Number(value || now.getUTCFullYear()))} />
        <NumberInput label={t('monthLabel')} value={month} min={1} max={12} onChange={(value) => setMonth(Number(value || 1))} />
        <Select
          label={t('categoryViewLabel')}
          data={typeOptions}
          value={distributionType}
          onChange={(value) => setDistributionType(value ?? String(CategoryType.Expense))}
        />
      </Group>

      <Card shadow="sm" radius="md" className="surface-card hero-card">
        <Text fw={600}>{t('summaryTitle')} ({baseCurrency})</Text>
        <Text c="dimmed" size="sm">
          {t('summaryCurrencyHint')}
        </Text>
        <div className="chart-box">
          <BarChart width={summaryChartWidth} height={chartHeight} data={summaryChartData}>
            <CartesianGrid strokeDasharray="2 6" stroke={darkChartGrid} />
            <XAxis dataKey="name" tick={{ fill: darkChartLabel, fontSize: 12 }} axisLine={{ stroke: darkChartGrid }} tickLine={false} />
            <YAxis tick={{ fill: darkChartLabel, fontSize: 12 }} axisLine={{ stroke: darkChartGrid }} tickLine={false} />
            <Tooltip formatter={formatCurrencyValue} contentStyle={tooltipStyle} />
            <Bar dataKey="value" name={`${t('amountWithCurrency')} (${baseCurrency})`}>
              {summaryChartData.map((entry, index) => (
                <Cell key={`${entry.name}-${index}`} fill={categoryColors[index % categoryColors.length]} />
              ))}
            </Bar>
          </BarChart>
        </div>
      </Card>

      <Card shadow="sm" radius="md" className="surface-card">
        <Text fw={600}>{t('categoryDistributionTitle')} ({baseCurrency})</Text>
        <Text c="dimmed" size="sm">
          {t('categoryDistributionHint')}
        </Text>
        <div className="split-grid report-charts">
          <div className="chart-box">
            <PieChart width={splitChartWidth} height={chartHeight}>
              <Pie
                dataKey="value"
                data={categoryChartData}
                nameKey="name"
                outerRadius={Math.max(80, Math.min(splitChartWidth, chartHeight) * 0.3)}
                label
                labelLine={false}
              >
                {categoryChartData.map((entry, index) => (
                  <Cell key={`${entry.name}-${index}`} fill={categoryColors[index % categoryColors.length]} />
                ))}
              </Pie>
              <Tooltip formatter={formatCurrencyValue} contentStyle={tooltipStyle} />
            </PieChart>
          </div>

          <div className="chart-box">
            <BarChart width={splitChartWidth} height={chartHeight} data={categoryChartData} layout="vertical" margin={{ left: 16, right: 16 }}>
              <CartesianGrid strokeDasharray="2 6" stroke={darkChartGrid} />
              <XAxis type="number" tick={{ fill: darkChartLabel, fontSize: 12 }} axisLine={{ stroke: darkChartGrid }} tickLine={false} />
              <YAxis dataKey="name" type="category" width={120} tick={{ fill: darkChartLabel, fontSize: 12 }} axisLine={{ stroke: darkChartGrid }} tickLine={false} />
              <Tooltip formatter={formatCurrencyValue} contentStyle={tooltipStyle} />
              <Bar dataKey="value" name={`${t('amountWithCurrency')} (${baseCurrency})`} fill="#2fc4db" />
            </BarChart>
          </div>
        </div>
        {!!topDistribution.length && (
          <div className="rows-stack">
            {topDistribution.map((item) => (
              <div key={item.name} className="list-row">
                <div className="list-row-main">
                  <span className="list-row-title">{item.name}</span>
                  <span className="list-row-meta">{((item.value / maxDistribution) * 100).toFixed(1)}%</span>
                  <div className="bar-track">
                    <div className="bar-fill" style={{ width: `${(item.value / maxDistribution) * 100}%` }} />
                  </div>
                </div>
                <span className="amount-positive">{baseCurrency} {item.value.toFixed(2)}</span>
              </div>
            ))}
          </div>
        )}

        {!categoryChartData.length && (
          <Stack gap="xs" mt="sm">
            <Text c="dimmed">{t('noCategoryData')}</Text>
          </Stack>
        )}
      </Card>
    </section>
  )
}
