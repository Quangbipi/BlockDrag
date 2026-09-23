---
name: find-bug
description: Use when deliberately auditing one bounded existing feature or flow for credible bugs, risks, gaps, or missing test coverage. Do not use for diagnosing a reported live symptom; use systematic-debugging instead.
---

# Find Bug

Audit one bounded flow. Evidence outranks speculation. Do not expand into adjacent flows without user authorization.

## Scope

Read and follow:

- `.claude/rules/bug-audit-workflow.md`
- `.claude/rules/bug-lifecycle-tracking.md`
- `.claude/rules/layer-architecture-dispatch.md`

Treat the flow named in the current user request as input. If missing, ask for one flow. If it names a whole system or project, propose an ordered list of bounded audits and ask which starts first.

State entry point, exit condition, owning layers, and excluded adjacent flows before auditing.

## Audit

Prefer `rg` and `rg --files` to map involved files, callers, callees, state transitions, data flow, teardown, and existing EditMode or AutoTest coverage. Review invalid and repeated inputs, ordering, races, retries, idempotency, pooling, subscriptions, teardown, save compatibility, configuration assumptions, silent failures, layer direction, test gaps, and runtime-only behavior.

Require concrete evidence for each credible finding: reproducible behavior, failing test, exception or log, user report, or precise risky code path with plausible impact. Report unsupported suspicions as considered and rejected; do not add them to the ledger.

Before recording a finding, search `.cursor/memory/mem-known-bugs-index.md` for duplicates. Apply the lifecycle rule for IDs, status, severity, evidence, and verification history.

Work inline by default. Delegate only when the user explicitly requests it.

## Deliverable

Report scope and entry-to-exit path, inspected files and tests, new or changed ledger IDs, rejected suspicions, untested boundaries, blocked verification, and one suggested next scope. Do not begin that scope without authorization.
