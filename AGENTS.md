# CozyLedger Agent Development Rules

These rules are mandatory for any coding agent working in this repository.

## Source of Truth

1. Always follow `doc/plan/DEV_PLAN.md` as the primary execution plan.
2. Use all relevant supporting docs in `doc/plan/` when implementing work.

## User Story Rules

1. When creating a user story, fully complete `doc/features/template.md`.
2. Store each user story file in the corresponding status folder under `doc/features/` (for example: `new`, `planned`, `finished`, etc.).
3. Do not place user story files directly in `doc/features/` root.

## Milestone Execution

1. Implement work milestone-by-milestone for the MVP.
2. After each completed milestone:
   - Commit changes with a clear milestone-focused message.
   - Push to the remote branch.
   - Continue immediately to the next milestone.

## Decision Handling Without Blocking

1. If a problem or ambiguity appears, do not pause to wait for user response.
2. Choose the best reasonable path forward and continue execution.
3. Record the decision and rationale in `plan\TO_DECIDE.md` for later review/justification.

## Testing Rule (Always Test New Development)

1. Always run relevant tests after every new development change before considering the task complete.
2. If no direct test exists for the change, run the closest related test suite and add/update tests when appropriate.
3. Report test results in the task summary, including any failures and the chosen remediation path.

## Documentation Rule (Docstring Coverage Required)

1. All new or modified code must include good docstrings for public classes, methods, functions, and modules.
2. Docstrings must clearly describe purpose, inputs, outputs, side effects, and important constraints.
3. Avoid placeholder docstrings; keep documentation accurate and updated with behavior changes.

## UI Interaction Rule (Action Menus)

1. Any create/edit form opened from a top-right action menu (three-dots menu) must be shown in a popup modal, not appended inline in the page flow.
2. Use the shared generic modal widget in `frontend/src/components/ActionFormModal.tsx` for these interactions instead of creating page-specific modal wrappers.

## UI Interaction Rule (List Details)

1. Clicking an item in a list should open a popup details view rather than appending details inline below or beside the list.
2. Use the shared details modal widget in `frontend/src/components/EntityDetailsModal.tsx` for read-only detail views.

## UI Input Rule (Currency)

1. Never use free-text currency entry for transactional or account workflows.
2. Use the shared dropdown component `frontend/src/components/CurrencySelect.tsx` for currency selection.
3. Currency selectors must include and default to the current book base currency.

## Stop Condition

1. Do not stop early.
2. Only stop when all MVP milestones defined in `doc/plan/DEV_PLAN.md` are complete.
