import { useQuery } from '@tanstack/react-query'
import { Card, Group, Loader, Text, Title } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { getJson } from '../lib/apiClient'

type HealthResponse = {
  status: string
}

export function DashboardPage() {
  const { t } = useTranslation()
  const healthQuery = useQuery({
    queryKey: ['health'],
    queryFn: () => getJson<HealthResponse>('/health'),
    staleTime: 30_000
  })

  return (
    <section className="page-panel">
      <Title order={2}>{t('dashboardTitle')}</Title>
      <Text className="page-hint">{t('dashboardHint')}</Text>
      <Card shadow="sm" radius="md" className="status-card">
        <Group>
          {healthQuery.isLoading && <Loader size="sm" />}
          <Text fw={600}>
            {healthQuery.data?.status === 'ok' ? t('healthOnline') : t('healthOffline')}
          </Text>
        </Group>
      </Card>
    </section>
  )
}
