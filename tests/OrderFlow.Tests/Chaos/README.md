# Chaos / Adversarial Tests

This folder contains experimental tests used to explore how OrderFlow
behaves under hostile or unexpected runtime conditions.

These tests are NOT part of the main project and are intentionally
excluded from CI execution.

The purpose of this branch is learning, not production hardening.

---

## Test: OutboxCrashAfterHandlerTests

**Scenario**

Simulates a runtime crash that occurs AFTER an outbox handler performs
a side-effect but BEFORE the message is marked as processed.

**What is being tested**

- Behavior of the outbox retry mechanism after partial execution
- Whether side-effects are duplicated when execution is retried

**Expected behavior**

- The outbox message is retried and eventually marked as processed

**Observed behavior**

- The side-effect is executed more than once

**Conclusion**

This test demonstrates that outbox handlers MUST be idempotent or
protected against re-execution.

The test is expected to fail and documents a known limitation of
non-idempotent handlers.

---

## Notes

Chaos tests may:
- Crash intentionally
- Use non-idiomatic code
- Rely on static state
- Fail by design

This is intentional and documented.
the transaction. This helps to verify that the outbox and its intentionally created for fail