# Data Dictionary (Draft)

> Planning only. No implementation details.

## User
- id
- email
- display_name
- locale

## Book
- id
- name
- base_currency
- created_at

## ExchangeRate (Planning)
- base_currency
- quote_currency
- rate
- effective_date
- source (external feed)

## Membership
- book_id
- user_id

## Account
- id
- book_id
- name_i18n (en, zh-Hans)
- type
- currency
- is_hidden
- include_in_net_worth
- note

## Category
- id
- book_id
- parent_id (nullable)
- name_i18n (en, zh-Hans)
- type (income|expense)
- is_active

## Transaction
- id
- book_id
- type (expense|income|transfer|balance_adjustment|liability_adjustment)
- date
- amount
- currency
- account_id
- to_account_id (transfer only)
- category_id (expense/income only)
- member_id (nullable)
- note (nullable)
- is_refund (bool)
- created_by_user_id
- created_at (UTC)

## Attachment
- id
- transaction_id
- file_name
- mime_type
- storage_path
- size_bytes
- original_size_bytes
- created_at

## Tag
- id
- book_id
- name_i18n (en, zh-Hans)

## TransactionTag
- transaction_id
- tag_id

## Optional Entities (Later)
- AuditLog
