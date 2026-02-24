# Feature User Story Template (Simple)

## 1) Basic Info

- **Feature ID**: `FEAT-BOOK-001`
- **Title**: `Manage books from Settings`
- **Status**: `new`

## 2) User Story

As an **authenticated user**, I want **book administration in Settings and a clear recovery path when I have no book membership** so that **I can always create/manage books from a centralized place and cannot accidentally use pages without an associated book**.

## 3) Requirements

- Add a dedicated book management section/page under the Settings tab for listing books the user can manage.
- Provide create, edit, and delete book actions from a top-right action menu on the book management page.
- If a user logs in and is not associated with any book, all app pages except book creation must be unavailable.
- In the no-book state, show a clear primary button that redirects the user to the book creation page.
- Any create/edit form launched from the action menu must open in a popup modal using `frontend/src/components/ActionFormModal.tsx`.
- Clicking a book in the list must open read-only details in a popup modal using `frontend/src/components/EntityDetailsModal.tsx`.
- Book create/edit flows that include currency selection must use `frontend/src/components/CurrencySelect.tsx`, including and defaulting to the current book base currency.
- Enforce permission checks so only authorized users can edit/delete a given book.

## 4) Acceptance Criteria

1. Given an authenticated user in Settings, when they open book management, then they can view a list of manageable books and trigger create/edit/delete actions from the top-right action menu.
2. Given the user selects create or edit from the action menu, when the form opens, then it is displayed in `ActionFormModal` and not inline in the page.
3. Given the user clicks a book in the list, when details are shown, then they appear in `EntityDetailsModal` and not inline in the page.
4. Given a logged-in user has zero associated books, when they try to access dashboard/ledger/reports/accounts/members/settings pages, then access is blocked and they are redirected to a no-book access state.
5. Given the user is in the no-book access state, when they click the primary action button, then they are redirected to the book creation page.

## 5) Implementation Notes

- Keep book lifecycle operations discoverable in one Settings location and remove dependency on login-time creation.
- Follow existing Settings navigation and access-control patterns.
- Reuse shared modal and selector components to preserve consistent UI behavior.
- Confirm deletion UX includes a clear confirmation path and handles membership/ownership constraints safely.

## 6) Tests

- **Tests to run**:
  - `n/a (story definition only)`
- **Coverage to add/update**:
  - `n/a`
- **Result summary**:
  - `n/a`

## 7) Done Checklist

- [ ] Requirements implemented.
- [ ] Acceptance criteria met.
- [ ] Tests added/updated and run.
- [ ] Docs updated if needed.
