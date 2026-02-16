import { Text, Title } from '@mantine/core'
import { useTranslation } from 'react-i18next'

export function LedgerPage() {
  const { t } = useTranslation()

  return (
    <section className="page-panel">
      <Title order={2}>{t('ledgerTitle')}</Title>
      <Text className="page-hint">{t('ledgerHint')}</Text>
    </section>
  )
}
