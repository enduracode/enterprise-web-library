---
name: ewl-external-integrations
description: Implement typed external API clients, complete paginated reads, external authentication, and per-user session lifecycle in EWL. Use for integration failure, expiry, concurrency, and user-isolation handling.
---

## Scope and workflow

Start with a small provider-specific client in Library, typed external contracts,
and operation-specific methods. Use `ewl-configuration` for installation settings,
`ewl-user-management` for local identity, and `ewl-authorization` for entry points.
Keep provider contracts and application-specific policies in system-owned skills.

1. Establish the approved target, version, authentication protocol, and account
   authority. Development installation does not authorize production calls.
2. Inspect installed EWL/TEWL abstractions before writing helpers. In particular,
   inspect `Tewl.Tools.HttpClientTools` retry/error behavior before using it for
   authenticated calls: errors must not expose credentials or provider bodies.
3. Give calls bounded deadlines/cancellation, validated response schemas, and
   explicit source failures. HTTP 200 may still contain a provider/RPC error.
4. Fetch all pages with deterministic ordering and explicit termination. Bound
   total work; hitting a limit means incomplete/failure, not success. Detect
   repeated/non-progressing pages and duplicate IDs. Counts alone do not guarantee
   a coherent snapshot while upstream records are changing.
5. Use a provider-supported snapshot/as-of boundary where possible. Otherwise
   document consistency limits and detect/reconcile changes; completing pagination
   does not establish a consistent snapshot. Preserve domain units and time semantics.
6. Retry only classified transient failures of eligible operations, within the
   overall deadline. Handle authentication rejection according to the provider's
   protocol and approved credential lifecycle, not blind retries or stored-password
   replay. Never blindly retry a non-idempotent write.

Keep external side effects outside long local database transactions where feasible.
An EWL transaction cannot roll back external API, email, or file-service operations.
External writes need provider-supported idempotency or durable operation tracking
and reconciliation of uncertain outcomes;
read-only evaluation does not prove write safety. OAuth integrations additionally
need caller-bound expiring single-use state and approved tenant/company validation.

## Per-user external sessions

When process-local storage meets the application's deployment requirements, use an
application-specific store implementing `PeriodicEvictionCompositeCacheEntry`.
Acquire the shared store under a static lock around the entire
`AppMemoryCache.GetCacheValue` call: concurrent factories can otherwise produce
competing stores. Do not use `UnderlyingCache` or add a second cache framework.
Revisit process-local storage for multiple processes; it is not a distributed store.

- Key sessions by trusted local identity and configured external target. Store tokens
  and necessary expiry metadata, never passwords. Use an injectable clock.
- Enforce provider expiry and any application-defined idle/absolute limits on every
  retrieval, including each paginated operation. Define what counts as activity;
  touching idle time must not extend an absolute deadline. At `now >= expiry`, do
  not use the expired credential. Periodic eviction is housekeeping, not enforcement
  of a security deadline.
- Synchronize lookup/touch, replacement, rejection invalidation, and eviction.
  Compare immutable session identity/generation before removal: an old rejected
  request or stale eviction must not delete a newer or freshly touched session.
  A concurrent dictionary alone does not make these compound operations atomic.
- Use per-request headers or isolated clients/cookie containers. Never put user
  tokens in shared mutable headers or shared cookies.
- Recheck current local authorization for each operation. A surviving local cookie
  does not prove the external session is valid. Define recovery for cache loss,
  expiry, or rejection according to the authentication model. Interactive sessions
  may require fresh login; OAuth may permit refresh under the approved grant.
  Synchronize refresh/replacement and protect refresh tokens as credentials. Do not
  silently replay stored passwords. Service credentials need their own approved
  acquisition/rotation policy rather than a per-user login flow.
- Invalidate sessions and in-flight publication on target changes, including A→B→A.
  Stable external identity mappings also need explicit reset/remapping when changing
  unrelated tenants/databases; numeric identifiers are not globally unique.

Choose provisioning, logout invalidation, session durations, and bootstrap policy
explicitly for the application. A cached session is not proof of fresh credential
validation. Inspect `Core/Caching/AppMemoryCache.cs` and
`Core/Caching/PeriodicEvictionCompositeCacheEntry.cs` in the EWL source, and verify
their contracts against the consuming package.

## Evaluation

Use synthetic transport/clock fixtures for multiple pages, duplicate/repeated page,
mid-stream failure, limit exhaustion, malformed/RPC error, timeout, expired session,
cross-user requests, replacement races, and target changes. Verify no secret appears
in errors/transcripts. Exercise an approved real test target separately before
claiming provider compatibility. See `ewl-agent-testing` for repeated agent probes.
