import {
  CategoryType,
} from './types'
import type {
  AccountResponse,
  AuthResponse,
  BookResponse,
  CategoryDistributionResponse,
  CategoryResponse,
  InviteResponse,
  MonthlySummaryResponse,
  TransactionResponse
} from './types'

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.trim() ?? ''

function buildUrl(path: string): string {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  if (!apiBaseUrl) {
    return normalizedPath
  }

  return `${apiBaseUrl.replace(/\/$/, '')}${normalizedPath}`
}

async function requestJson<T>(
  path: string,
  init: RequestInit = {},
  token?: string
): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  if (!(init.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json')
  }

  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(buildUrl(path), {
    ...init,
    headers
  })

  if (!response.ok) {
    const text = await response.text()
    throw new Error(text || `Request failed: ${response.status}`)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

/**
 * Registers a new user and returns a JWT token payload.
 */
export function register(email: string, password: string): Promise<AuthResponse> {
  return requestJson<AuthResponse>('/auth/register', {
    method: 'POST',
    body: JSON.stringify({ email, password, displayName: email, locale: 'en' })
  })
}

/**
 * Logs in an existing user and returns a JWT token payload.
 */
export function login(email: string, password: string): Promise<AuthResponse> {
  return requestJson<AuthResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password })
  })
}

/**
 * Creates a new book for the authenticated user.
 */
export function createBook(token: string, name: string, baseCurrency: string): Promise<BookResponse> {
  return requestJson<BookResponse>(
    '/books',
    {
      method: 'POST',
      body: JSON.stringify({ name, baseCurrency })
    },
    token
  )
}

/**
 * Lists accounts in the selected book.
 */
export function listAccounts(token: string, bookId: string): Promise<AccountResponse[]> {
  return requestJson<AccountResponse[]>(`/books/${bookId}/accounts`, {}, token)
}

/**
 * Creates a new account in the selected book.
 */
export function createAccount(
  token: string,
  bookId: string,
  payload: Omit<AccountResponse, 'id'>
): Promise<AccountResponse> {
  return requestJson<AccountResponse>(
    `/books/${bookId}/accounts`,
    {
      method: 'POST',
      body: JSON.stringify(payload)
    },
    token
  )
}

/**
 * Updates an existing account in the selected book.
 */
export function updateAccount(
  token: string,
  bookId: string,
  accountId: string,
  payload: Omit<AccountResponse, 'id'>
): Promise<AccountResponse> {
  return requestJson<AccountResponse>(
    `/books/${bookId}/accounts/${accountId}`,
    {
      method: 'PUT',
      body: JSON.stringify(payload)
    },
    token
  )
}

/**
 * Lists categories in the selected book.
 */
export function listCategories(token: string, bookId: string): Promise<CategoryResponse[]> {
  return requestJson<CategoryResponse[]>(`/books/${bookId}/categories`, {}, token)
}

/**
 * Creates a new category in the selected book.
 */
export function createCategory(
  token: string,
  bookId: string,
  payload: Omit<CategoryResponse, 'id'>
): Promise<CategoryResponse> {
  return requestJson<CategoryResponse>(
    `/books/${bookId}/categories`,
    {
      method: 'POST',
      body: JSON.stringify(payload)
    },
    token
  )
}

/**
 * Updates an existing category in the selected book.
 */
export function updateCategory(
  token: string,
  bookId: string,
  categoryId: string,
  payload: Omit<CategoryResponse, 'id'>
): Promise<CategoryResponse> {
  return requestJson<CategoryResponse>(
    `/books/${bookId}/categories/${categoryId}`,
    {
      method: 'PUT',
      body: JSON.stringify(payload)
    },
    token
  )
}

/**
 * Lists transactions in the selected book.
 */
export function listTransactions(token: string, bookId: string): Promise<TransactionResponse[]> {
  return requestJson<TransactionResponse[]>(`/books/${bookId}/transactions`, {}, token)
}

/**
 * Creates a transaction in the selected book.
 */
export function createTransaction(
  token: string,
  bookId: string,
  payload: Omit<TransactionResponse, 'id' | 'createdAtUtc'>
): Promise<TransactionResponse> {
  return requestJson<TransactionResponse>(
    `/books/${bookId}/transactions`,
    {
      method: 'POST',
      body: JSON.stringify(payload)
    },
    token
  )
}

/**
 * Updates a transaction in the selected book.
 */
export function updateTransaction(
  token: string,
  bookId: string,
  transactionId: string,
  payload: Omit<TransactionResponse, 'id' | 'createdAtUtc'>
): Promise<TransactionResponse> {
  return requestJson<TransactionResponse>(
    `/books/${bookId}/transactions/${transactionId}`,
    {
      method: 'PUT',
      body: JSON.stringify(payload)
    },
    token
  )
}

/**
 * Generates a membership invite for the selected book.
 */
export function createInvite(token: string, bookId: string): Promise<InviteResponse> {
  return requestJson<InviteResponse>(
    `/books/${bookId}/invites`,
    {
      method: 'POST',
      body: JSON.stringify({})
    },
    token
  )
}

/**
 * Accepts an invite token and joins the target book.
 */
export function acceptInvite(token: string, inviteToken: string): Promise<{ bookId: string }> {
  return requestJson<{ bookId: string }>(
    `/invites/${inviteToken}/accept`,
    {
      method: 'POST'
    },
    token
  )
}

/**
 * Fetches monthly summary totals for the selected book.
 */
export function getMonthlySummary(
  token: string,
  bookId: string,
  year: number,
  month: number
): Promise<MonthlySummaryResponse> {
  return requestJson<MonthlySummaryResponse>(
    `/books/${bookId}/reports/summary/monthly?year=${year}&month=${month}`,
    {},
    token
  )
}

/**
 * Fetches category distribution totals for the selected book and month.
 */
export function getCategoryDistribution(
  token: string,
  bookId: string,
  year: number,
  month: number,
  type: CategoryType
): Promise<CategoryDistributionResponse> {
  const typeLabel = type === CategoryType.Income ? 'Income' : 'Expense'
  return requestJson<CategoryDistributionResponse>(
    `/books/${bookId}/reports/categories?year=${year}&month=${month}&type=${typeLabel}`,
    {},
    token
  )
}
