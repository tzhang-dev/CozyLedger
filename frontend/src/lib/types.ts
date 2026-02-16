/**
 * Account type values aligned with backend enum numeric values.
 */
export const AccountType = {
  Cash: 0,
  Bank: 1,
  CreditCard: 2,
  Investment: 3,
  Liability: 4,
  Other: 5
} as const

export type AccountType = (typeof AccountType)[keyof typeof AccountType]

/**
 * Category type values aligned with backend enum numeric values.
 */
export const CategoryType = {
  Income: 0,
  Expense: 1
} as const

export type CategoryType = (typeof CategoryType)[keyof typeof CategoryType]

/**
 * Transaction type values aligned with backend enum numeric values.
 */
export const TransactionType = {
  Expense: 0,
  Income: 1,
  Transfer: 2,
  BalanceAdjustment: 3,
  LiabilityAdjustment: 4
} as const

export type TransactionType = (typeof TransactionType)[keyof typeof TransactionType]

export type AuthResponse = {
  token: string
  expiresAtUtc: string
}

export type BookResponse = {
  id: string
  name: string
  baseCurrency: string
}

export type AccountResponse = {
  id: string
  nameEn: string
  nameZhHans: string
  type: AccountType
  currency: string
  isHidden: boolean
  includeInNetWorth: boolean
  note?: string | null
}

export type CategoryResponse = {
  id: string
  nameEn: string
  nameZhHans: string
  type: CategoryType
  parentId?: string | null
  isActive: boolean
}

export type TransactionResponse = {
  id: string
  type: TransactionType
  dateUtc: string
  amount: number
  currency: string
  accountId: string
  toAccountId?: string | null
  categoryId?: string | null
  memberId?: string | null
  note?: string | null
  isRefund: boolean
  createdAtUtc: string
}

export type InviteResponse = {
  token: string
  inviteUrl: string
  expiresAtUtc: string
}

export type MonthlySummaryResponse = {
  baseCurrency: string
  periodStartUtc: string
  periodEndExclusiveUtc: string
  incomeTotal: number
  expenseTotal: number
  netTotal: number
}

export type CategoryDistributionResponse = {
  baseCurrency: string
  periodStartUtc: string
  periodEndExclusiveUtc: string
  type: CategoryType
  items: Array<{
    categoryId: string
    categoryNameEn: string
    categoryNameZhHans: string
    totalBaseAmount: number
  }>
}
