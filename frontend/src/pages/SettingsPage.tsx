import { ActionIcon, Button, Card, Group, Text, Title } from '@mantine/core'
import { IconChevronRight, IconLanguage, IconTags } from '@tabler/icons-react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'

type Props = {
  onSignOut: () => void
}

/**
 * Provides user-level application settings such as language and sign-out actions.
 */
export function SettingsPage({ onSignOut }: Props) {
  const { i18n, t } = useTranslation()
  const navigate = useNavigate()

  return (
    <section className="page-panel">
      <div className="page-titlebar">
        <div>
          <Title order={2}>{t('settingsTitle')}</Title>
          <p>{t('settingsHint')}</p>
        </div>
      </div>
      <div className="split-grid">
        <Card shadow="sm" radius="md" className="surface-card form-grid">
          <Text fw={600}>{t('languageLabel')}</Text>
          <Group gap="xs">
            <IconLanguage size={18} />
            <ActionIcon
              variant={i18n.language === 'en' ? 'filled' : 'light'}
              onClick={() => void i18n.changeLanguage('en')}
              aria-label="Switch to English"
            >
              EN
            </ActionIcon>
            <ActionIcon
              variant={i18n.language === 'zh' ? 'filled' : 'light'}
              onClick={() => void i18n.changeLanguage('zh')}
              aria-label="切换到中文"
            >
              中
            </ActionIcon>
          </Group>
        </Card>
        <Card shadow="sm" radius="md" className="surface-card form-grid">
          <Text fw={600}>{t('categoriesTitle')}</Text>
          <Button variant="light" leftSection={<IconTags size={16} />} rightSection={<IconChevronRight size={14} />} onClick={() => navigate('/settings/categories')}>
            {t('manageCategories')}
          </Button>
        </Card>
        <Card shadow="sm" radius="md" className="surface-card form-grid">
          <Text fw={600}>{t('accountActionsTitle')}</Text>
          <Button onClick={onSignOut}>{t('signOut')}</Button>
        </Card>
      </div>
    </section>
  )
}
