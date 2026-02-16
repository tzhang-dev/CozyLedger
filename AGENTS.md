# CozyLedger Agent Development Rules

These rules are mandatory for any coding agent working in this repository.

## Source of Truth

1. Always follow `doc/plan/DEV_PLAN.md` as the primary execution plan.
2. Use all relevant supporting docs in `doc/plan/` when implementing work.

## Milestone Execution

1. Implement work milestone-by-milestone for the MVP.
2. After each completed milestone:
   - Commit changes with a clear milestone-focused message.
   - Push to the remote branch.
   - Continue immediately to the next milestone.

## Decision Handling Without Blocking

1. If a problem or ambiguity appears, do not pause to wait for user response.
2. Choose the best reasonable path forward and continue execution.
3. Record the decision and rationale in `TO_DECIDE.md` for later review/justification.

## Testing Rule (Always Test New Development)

1. Always run relevant tests after every new development change before considering the task complete.
2. If no direct test exists for the change, run the closest related test suite and add/update tests when appropriate.
3. Report test results in the task summary, including any failures and the chosen remediation path.

## Stop Condition

1. Do not stop early.
2. Only stop when all MVP milestones defined in `doc/plan/DEV_PLAN.md` are complete.
