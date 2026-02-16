import { Text, Title } from '@mantine/core'
import { useTranslation } from 'react-i18next'

export function AccountsPage() {
  const { t } = useTranslation()

  return (
    <section className="page-panel">
      <Title order={2}>{t('accountsTitle')}</Title>
      <Text className="page-hint">{t('accountsHint')}</Text>
    </section>
  )
}
