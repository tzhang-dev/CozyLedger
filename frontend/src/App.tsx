import { useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { NavLink, Navigate, Route, Routes, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { Alert, Button, Card, Group, PasswordInput, Text, TextInput } from '@mantine/core'
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
  className?: string
  end?: boolean
}

function Navigation({ items }: { items: NavItem[] }) {
  return (
    <nav className="bottom-nav">
      {items.map((item) => (
        <NavLink
          key={`${item.to}-${item.label}`}
          to={item.to}
          end={item.end}
          className={({ isActive }) =>
            [
              'bottom-nav-link',
              item.className ?? '',
              isActive ? 'bottom-nav-link-active' : ''
            ]
              .filter(Boolean)
              .join(' ')
          }
        >
          {item.label}
        </NavLink>
      ))}
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
      // Keep setup screen usable even when book lookup fails.
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
    <main className="setup-main">
      <Card shadow="sm" radius="md" className="setup-card">
        <Text fw={700} size="lg">
          {t('setupTitle')}
        </Text>
        <Text c="dimmed" size="sm">
          {t('setupHint')}
        </Text>
        <form onSubmit={handleRegister} className="form-grid">
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
        <form onSubmit={handleCreateBook} className="form-grid">
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
      { to: '/ledger', label: t('navTransactions'), end: true },
      { to: '/reports', label: t('navReports') },
      { to: '/ledger/new', label: t('navNewTransaction'), className: 'bottom-nav-link-center' },
      { to: '/accounts', label: t('navAccounts') },
      { to: '/settings', label: t('navSettings') }
    ],
    [t]
  )

  if (!session) {
    return <SetupPanel onReady={setSession} />
  }

  return (
    <div className="app-shell">
      <main className="app-main">
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
          <Route path="*" element={<Navigate to="/ledger" replace />} />
        </Routes>
      </main>

      <Navigation items={navItems} />
    </div>
  )
}

export default App
