import { NavLink, Navigate, Route, Routes } from 'react-router-dom'
import { ActionIcon, Group, Text } from '@mantine/core'
import { IconLanguage } from '@tabler/icons-react'
import { useTranslation } from 'react-i18next'
import { DashboardPage } from './pages/DashboardPage'
import { LedgerPage } from './pages/LedgerPage'
import { ReportsPage } from './pages/ReportsPage'
import { AccountsPage } from './pages/AccountsPage'
import { MembersPage } from './pages/MembersPage'
import './App.css'

type NavItem = {
  to: string
  label: string
}

function LanguageSwitch() {
  const { i18n, t } = useTranslation()

  return (
    <Group gap="xs">
      <IconLanguage size={18} />
      <Text size="sm" fw={600}>
        {t('languageLabel')}
      </Text>
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
  )
}

function Navigation({ items }: { items: NavItem[] }) {
  return (
    <>
      <nav className="desktop-nav">
        {items.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) =>
              isActive ? 'nav-link nav-link-active' : 'nav-link'
            }
          >
            {item.label}
          </NavLink>
        ))}
      </nav>
      <nav className="mobile-nav">
        {items.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) =>
              isActive ? 'mobile-nav-link mobile-nav-link-active' : 'mobile-nav-link'
            }
          >
            {item.label}
          </NavLink>
        ))}
      </nav>
    </>
  )
}

function App() {
  const { t } = useTranslation()

  const navItems: NavItem[] = [
    { to: '/dashboard', label: t('navDashboard') },
    { to: '/ledger', label: t('navLedger') },
    { to: '/reports', label: t('navReports') },
    { to: '/accounts', label: t('navAccounts') },
    { to: '/members', label: t('navMembers') }
  ]

  return (
    <div className="app-shell">
      <header className="app-header">
        <h1>{t('appTitle')}</h1>
        <LanguageSwitch />
      </header>

      <Navigation items={navItems} />

      <main className="app-main">
        <Routes>
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/ledger" element={<LedgerPage />} />
          <Route path="/reports" element={<ReportsPage />} />
          <Route path="/accounts" element={<AccountsPage />} />
          <Route path="/members" element={<MembersPage />} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </main>
    </div>
  )
}

export default App
