import { Button } from '@mantine/core'
import { IconChevronRight, IconLanguage, IconLogout, IconTags, IconUsers, IconWallet } from '@tabler/icons-react'
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
    <section className="cl-page">
      <header className="cl-header">
        <div className="cl-header-inner">
          <h1 className="cl-header-title">{t('settingsTitle')}</h1>
          <p className="cl-header-subtitle">{t('settingsHint')}</p>
        </div>
      </header>

      <div className="cl-body">
        <div className="cl-card cl-form-grid">
          <p className="cl-card-title">{t('languageLabel')}</p>
          <div className="cl-filter-row">
            <button type="button" className={`cl-chip ${i18n.language === 'en' ? 'cl-chip-active' : ''}`} onClick={() => void i18n.changeLanguage('en')}>
              EN
            </button>
            <button type="button" className={`cl-chip ${i18n.language === 'zh' ? 'cl-chip-active' : ''}`} onClick={() => void i18n.changeLanguage('zh')}>
              中文
            </button>
          </div>
        </div>

        <div className="cl-card cl-settings-list-card">
          <button type="button" className="cl-list-row cl-settings-row" onClick={() => navigate('/accounts')}>
            <span className="cl-list-row-main">
              <span className="cl-list-row-title cl-settings-row-title"><IconWallet size={15} /> {t('accountsTitle')}</span>
              <span className="cl-list-row-meta">{t('settingsAccountsHint')}</span>
            </span>
            <IconChevronRight size={14} className="cl-settings-row-arrow" />
          </button>
          <button type="button" className="cl-list-row cl-settings-row" onClick={() => navigate('/members')}>
            <span className="cl-list-row-main">
              <span className="cl-list-row-title cl-settings-row-title"><IconUsers size={15} /> {t('navMembers')}</span>
              <span className="cl-list-row-meta">{t('settingsMembersHint')}</span>
            </span>
            <IconChevronRight size={14} className="cl-settings-row-arrow" />
          </button>
          <button type="button" className="cl-list-row cl-settings-row" onClick={() => navigate('/settings/categories')}>
            <span className="cl-list-row-main">
              <span className="cl-list-row-title cl-settings-row-title"><IconTags size={15} /> {t('categoriesTitle')}</span>
              <span className="cl-list-row-meta">{t('settingsCategoriesHint')}</span>
            </span>
            <IconChevronRight size={14} className="cl-settings-row-arrow" />
          </button>
          <button type="button" className="cl-list-row cl-settings-row" onClick={() => void i18n.changeLanguage(i18n.language === 'en' ? 'zh' : 'en')}>
            <span className="cl-list-row-main">
              <span className="cl-list-row-title cl-settings-row-title"><IconLanguage size={15} /> {t('settingsQuickLanguageToggle')}</span>
              <span className="cl-list-row-meta">{t('settingsQuickLanguageHint')}</span>
            </span>
            <IconChevronRight size={14} className="cl-settings-row-arrow" />
          </button>
        </div>

        <div className="cl-card cl-form-grid">
          <p className="cl-card-title">{t('accountActionsTitle')}</p>
          <Button color="red" leftSection={<IconLogout size={16} />} onClick={onSignOut}>
            {t('signOut')}
          </Button>
        </div>
      </div>
    </section>
  )
}
