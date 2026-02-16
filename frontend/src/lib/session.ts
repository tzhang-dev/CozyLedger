/**
 * Represents persisted auth and current book selection for frontend API calls.
 */
export type SessionState = {
  token: string
  bookId: string
}

const SESSION_KEY = 'cozyledger.session'

/**
 * Loads the current session from local storage.
 * Returns null when missing or malformed.
 */
export function loadSession(): SessionState | null {
  const raw = localStorage.getItem(SESSION_KEY)
  if (!raw) {
    return null
  }

  try {
    const parsed = JSON.parse(raw) as Partial<SessionState>
    if (!parsed.token || !parsed.bookId) {
      return null
    }

    return {
      token: parsed.token,
      bookId: parsed.bookId
    }
  } catch {
    return null
  }
}

/**
 * Persists the current session to local storage.
 */
export function saveSession(session: SessionState): void {
  localStorage.setItem(SESSION_KEY, JSON.stringify(session))
}

/**
 * Removes the current session from local storage.
 */
export function clearSession(): void {
  localStorage.removeItem(SESSION_KEY)
}
