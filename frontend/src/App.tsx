import { useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { NavLink, Navigate, Route, Routes } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { ActionIcon, Button, Card, Group, PasswordInput, Text, TextInput } from '@mantine/core'
import { IconLanguage } from '@tabler/icons-react'
import { useTranslation } from 'react-i18next'
import { createBook, login, register } from './lib/cozyApi'
import { clearSession, loadSession, saveSession } from './lib/session'
import type { SessionState } from './lib/session'
import { AccountsPage } from './pages/AccountsPage'
import { DashboardPage } from './pages/DashboardPage'
import { LedgerPage } from './pages/LedgerPage'
import { MembersPage } from './pages/MembersPage'
import { ReportsPage } from './pages/ReportsPage'
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

function SetupPanel({ onReady }: { onReady: (session: SessionState) => void }) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [bookName, setBookName] = useState('Household')
  const [baseCurrency, setBaseCurrency] = useState('USD')
  const [token, setToken] = useState('')

  const registerMutation = useMutation({
    mutationFn: () => register(email, password),
    onSuccess: (result) => setToken(result.token)
  })

  const loginMutation = useMutation({
    mutationFn: () => login(email, password),
    onSuccess: (result) => setToken(result.token)
  })

  const createBookMutation = useMutation({
    mutationFn: () => createBook(token, bookName, baseCurrency),
    onSuccess: (result) => {
      const session = { token, bookId: result.id }
      saveSession(session)
      onReady(session)
    }
  })

  const handleRegister = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    registerMutation.mutate()
  }

  const handleCreateBook = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!token.trim()) {
      return
    }

    createBookMutation.mutate()
  }

  return (
    <main className="setup-main">
      <Card shadow="sm" radius="md" className="setup-card">
        <Text fw={700} size="lg">
          Get Started
        </Text>
        <Text c="dimmed" size="sm">
          Register or login first, then create your first book.
        </Text>
        <form onSubmit={handleRegister} className="form-grid">
          <TextInput label="Email" value={email} onChange={(event) => setEmail(event.currentTarget.value)} required />
          <PasswordInput label="Password" value={password} onChange={(event) => setPassword(event.currentTarget.value)} required />
          <Group>
            <Button type="submit" loading={registerMutation.isPending}>
              Register
            </Button>
            <Button type="button" variant="light" onClick={() => loginMutation.mutate()} loading={loginMutation.isPending}>
              Login
            </Button>
          </Group>
        </form>
        <form onSubmit={handleCreateBook} className="form-grid">
          <TextInput label="Book name" value={bookName} onChange={(event) => setBookName(event.currentTarget.value)} required />
          <TextInput
            label="Base currency"
            value={baseCurrency}
            onChange={(event) => setBaseCurrency(event.currentTarget.value.toUpperCase())}
            maxLength={3}
            required
          />
          <TextInput label="Token" value={token} onChange={(event) => setToken(event.currentTarget.value)} required />
          <Button type="submit" loading={createBookMutation.isPending}>
            Create book
          </Button>
        </form>
      </Card>
    </main>
  )
}

/**
 * Root application shell handling session bootstrap and route layout.
 */
function App() {
  const { t } = useTranslation()
  const [session, setSession] = useState<SessionState | null>(() => loadSession())

  const navItems: NavItem[] = useMemo(
    () => [
      { to: '/dashboard', label: t('navDashboard') },
      { to: '/ledger', label: t('navLedger') },
      { to: '/reports', label: t('navReports') },
      { to: '/accounts', label: t('navAccounts') },
      { to: '/members', label: t('navMembers') }
    ],
    [t]
  )

  if (!session) {
    return <SetupPanel onReady={setSession} />
  }

  return (
    <div className="app-shell">
      <header className="app-header">
        <h1>{t('appTitle')}</h1>
        <Group>
          <LanguageSwitch />
          <Button
            variant="light"
            onClick={() => {
              clearSession()
              setSession(null)
            }}
          >
            Sign out
          </Button>
        </Group>
      </header>

      <Navigation items={navItems} />

      <main className="app-main">
        <Routes>
          <Route path="/dashboard" element={<DashboardPage token={session.token} bookId={session.bookId} />} />
          <Route path="/ledger" element={<LedgerPage token={session.token} bookId={session.bookId} />} />
          <Route path="/reports" element={<ReportsPage token={session.token} bookId={session.bookId} />} />
          <Route path="/accounts" element={<AccountsPage token={session.token} bookId={session.bookId} />} />
          <Route
            path="/members"
            element={<MembersPage token={session.token} bookId={session.bookId} onBookJoined={(bookId) => setSession({ ...session, bookId })} />}
          />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </main>
    </div>
  )
}

export default App
