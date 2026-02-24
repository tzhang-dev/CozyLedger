# Feature User Story Template (Simple)

## 1) Basic Info

- **Feature ID**: `FEAT-AUTH-001`
- **Title**: `Simplify login page to email + password only`
- **Status**: `new`

## 2) User Story

As an **existing user**, I want **the login page to only ask for email and password** so that **authentication is separated from book setup and the entry experience is clear**.

## 3) Requirements

- The login page must only contain authentication fields required to sign in (`email` and `password`) and related validation/error states.
- Registration/first-time account creation is not supported in this flow and must not be shown on the login page.
- Book creation must not appear on the login page.
- After successful authentication, if the user has no associated book memberships, the app must redirect to a dedicated post-login "no book" flow/page.
- If the user has at least one associated book, the app must route directly to the normal authenticated landing flow.
- API/client auth handling must continue to issue/store session credentials exactly once per login and preserve current security checks.
- The login page UI must be reworked to match the current product visual language (layout, spacing, typography, controls, and interaction patterns).

## 4) Acceptance Criteria

1. Given an unauthenticated user on the login route, when the page loads, then only email and password login controls are shown and no registration or book creation controls are visible.
2. Given a user who logs in successfully and has no associated books, when authentication completes, then they are redirected away from login to a dedicated no-book page/flow.
3. Given a user who logs in successfully and has one or more associated books, when authentication completes, then they are routed to the standard authenticated landing page without any setup interruption.

## 5) Implementation Notes

- Keep login responsibilities strictly scoped to authentication to reduce onboarding confusion.
- Reuse existing auth form patterns and validation UX where possible.
- Reuse shared design tokens/components and avoid introducing one-off styling patterns for login.
- Coordinate with routing guards to ensure "no-book" users are handled post-login, not during authentication.
- Ensure copy and i18n labels reflect the new flow (no book creation language on login).

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
