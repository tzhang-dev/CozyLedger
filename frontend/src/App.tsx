import { useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { NavLink, Navigate, Route, Routes, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { Alert, Button, Card, Group, PasswordInput, Text, TextInput } from '@mantine/core'
import {
  IconChartBar,
  IconHome,
  IconList,
  IconPlus,
  IconSettings
} from '@tabler/icons-react'
import { useTranslation } from 'react-i18next'
import { ApiError, createBook, isNetworkError, listBooks, login, register } from './lib/cozyApi'
import { clearSession, loadSession, saveSession } from './lib/session'
import type { SessionState } from './lib/session'
import { AccountsPage } from './pages/AccountsPage'
import { CategoriesSettingsPage } from './pages/CategoriesSettingsPage'
import { DashboardPage } from './pages/DashboardPage'
import { LedgerPage } from './pages/LedgerPage'
import { MembersPage } from './pages/MembersPage'
import { ReportsPage } from './pages/ReportsPage'
import { SettingsPage } from './pages/SettingsPage'
import './App.css'

type NavItem = {
  to: string
  label: string
  icon: (typeof IconHome)
  end?: boolean
  center?: boolean
}

function Navigation({ items }: { items: NavItem[] }) {
  return (
    <nav className="cl-bottom-nav" aria-label="Primary">
      {items.map((item) => {
        const Icon = item.icon
        return (
          <NavLink
            key={`${item.to}-${item.label}`}
            to={item.to}
            end={item.end}
            className={({ isActive }) => [
              'cl-bottom-nav-item',
              item.center ? 'cl-bottom-nav-item-center' : '',
              isActive ? 'cl-bottom-nav-item-active' : ''
            ].join(' ')}
          >
            <Icon size={item.center ? 24 : 19} stroke={1.8} />
            {!item.center ? <span>{item.label}</span> : null}
          </NavLink>
        )
      })}
    </nav>
  )
}

function SetupPanel({ onReady }: { onReady: (session: SessionState) => void }) {
  const { t } = useTranslation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [bookName, setBookName] = useState('Household')
  const [baseCurrency, setBaseCurrency] = useState('USD')
  const [token, setToken] = useState('')
  const [loginError, setLoginError] = useState<string | null>(null)

  const registerMutation = useMutation({
    mutationFn: () => register(email, password),
    onSuccess: (result) => setToken(result.token)
  })

  /**
   * Loads an existing book and enters the app immediately when available.
   */
  const tryEnterExistingBook = async (userToken: string) => {
    try {
      const books = await listBooks(userToken)
      const firstBook = books[0]
      if (!firstBook) {
        return
      }

      const session = { token: userToken, bookId: firstBook.id }
      saveSession(session)
      onReady(session)
    } catch {
      // Setup panel remains usable if the auto-enter attempt fails.
    }
  }

  const loginMutation = useMutation({
    mutationFn: () => login(email, password),
    onSuccess: async (result) => {
      setLoginError(null)
      setToken(result.token)
      await tryEnterExistingBook(result.token)
    },
    onError: (error: unknown) => {
      if (isNetworkError(error)) {
        setLoginError(t('loginErrorNetwork'))
        return
      }

      if (error instanceof ApiError && (error.status === 401 || error.status === 403)) {
        setLoginError(t('loginErrorAuth'))
        return
      }

      setLoginError(t('loginErrorGeneric'))
    }
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
    <main className="cl-setup-shell">
      <div className="cl-setup-deco cl-setup-deco-right" />
      <div className="cl-setup-deco cl-setup-deco-left" />
      <Card shadow="sm" radius="lg" className="cl-setup-card">
        <Text className="cl-setup-title">{t('setupTitle')}</Text>
        <Text className="cl-setup-subtitle">{t('setupHint')}</Text>

        <form onSubmit={handleRegister} className="cl-form-grid">
          <TextInput
            label={t('emailLabel')}
            value={email}
            onChange={(event) => {
              setEmail(event.currentTarget.value)
              setLoginError(null)
            }}
            required
          />
          <PasswordInput
            label={t('passwordLabel')}
            value={password}
            onChange={(event) => {
              setPassword(event.currentTarget.value)
              setLoginError(null)
            }}
            required
          />
          <Group>
            <Button type="submit" loading={registerMutation.isPending}>
              {t('registerButton')}
            </Button>
            <Button type="button" variant="light" onClick={() => loginMutation.mutate()} loading={loginMutation.isPending}>
              {t('loginButton')}
            </Button>
          </Group>
          {loginError ? <Alert color="red">{loginError}</Alert> : null}
        </form>

        <form onSubmit={handleCreateBook} className="cl-form-grid">
          <TextInput label={t('bookNameLabel')} value={bookName} onChange={(event) => setBookName(event.currentTarget.value)} required />
          <TextInput
            label={t('baseCurrencyLabel')}
            value={baseCurrency}
            onChange={(event) => setBaseCurrency(event.currentTarget.value.toUpperCase())}
            maxLength={3}
            required
          />
          <TextInput label={t('tokenLabel')} value={token} onChange={(event) => setToken(event.currentTarget.value)} required />
          <Button type="submit" loading={createBookMutation.isPending}>
            {t('createBookButton')}
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
  const navigate = useNavigate()

  const navItems: NavItem[] = useMemo(
    () => [
      { to: '/dashboard', label: t('navDashboard'), icon: IconHome },
      { to: '/ledger', label: t('navTransactions'), icon: IconList },
      { to: '/ledger/new', label: t('navNewTransaction'), icon: IconPlus, center: true },
      { to: '/reports', label: t('navReports'), icon: IconChartBar },
      { to: '/settings', label: t('navSettings'), icon: IconSettings }
    ],
    [t]
  )

  if (!session) {
    return <SetupPanel onReady={setSession} />
  }

  return (
    <div className="cl-mobile-shell-bg">
      <div className="cl-mobile-shell">
        <main className="cl-mobile-main">
          <Routes>
            <Route path="/dashboard" element={<DashboardPage token={session.token} bookId={session.bookId} />} />
            <Route path="/ledger" element={<LedgerPage token={session.token} bookId={session.bookId} mode="list" />} />
            <Route path="/ledger/new" element={<LedgerPage token={session.token} bookId={session.bookId} mode="new" />} />
            <Route path="/reports" element={<ReportsPage token={session.token} bookId={session.bookId} />} />
            <Route path="/accounts" element={<AccountsPage token={session.token} bookId={session.bookId} />} />
            <Route
              path="/members"
              element={<MembersPage token={session.token} bookId={session.bookId} onBookJoined={(bookId) => setSession({ ...session, bookId })} />}
            />
            <Route
              path="/settings"
              element={
                <SettingsPage
                  onSignOut={() => {
                    clearSession()
                    setSession(null)
                  }}
                />
              }
            />
            <Route path="/settings/categories" element={<CategoriesSettingsPage token={session.token} bookId={session.bookId} onBack={() => navigate('/settings')} />} />
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Routes>
        </main>

        <Navigation items={navItems} />
      </div>
    </div>
  )
}

export default App
