---
name: ewl-authorization
description: Protect EWL pages, exports, supporting resources, and domain operations with shared authentication, role, account-eligibility, and ownership checks. Use when implementing or reviewing access restrictions or direct denied access.
---

## Workflow

1. Enumerate entry points: page, PDF/Excel download, supporting resource, postback,
   custom endpoint, and any worker. Write down who may view versus modify each.
2. Use an authenticated resource/entity-setup parent and small named domain checks.
   Consult `ewl-page` for declarations/routing and `ewl-user-management` for identity.
   `ResourceBase.userCanAccess` defaults to true. Parent/entity-setup restrictions
   run before child restrictions; navigation visibility is not authorization.
3. Place each export under the protected hierarchy or explicitly apply the same
   policy. Check authorization before retrieving source data or producing bytes.
   An `EwfResponse` alone does not authorize anything. Avoid public static files.
4. Include current account eligibility in the policy/provider contract, such as
   active status when the system supports deactivation. A surviving
   authentication cookie or cached external session does not grant current access.
5. Resolve ownership server-side; do not accept a submitted owner/UserId as the
   caller. When using external identity, qualify its stable UID by the configured
   target/tenant rather than treating it as globally unique.
6. Reuse business authorization in operations called outside resource dispatch.
   A worker must revalidate its execution authority separately from its requester.
   Add only the operations actually needed; do not build a generic permission engine.

## Example: a protected page and download

For a system with private reports, define a small policy checking authentication,
current account eligibility, and report permission. An illustrative protected parent uses:

```csharp
protected override bool userCanAccess => ReportAccess.CanView( SystemUser.Current );
```

`ReportAccess.CanView` is an example of system-owned code, not an EWL API or a
required policy name. Both page and download
must inherit this parent (verify `createParent` and entity setup), or use the same
policy themselves. An owner-bound saved report additionally checks the report owner
against the current user before loading its snapshot. Check again when executing a
modification whose authority/state could have changed since the page was rendered.

The URL parent (`getUrlParent`) may differ from the authorization parent
(`createParent`); verify both rather than inferring access from URL nesting. See
`ewl-reporting`'s export reference for an illustrative page/download pattern.

Do not introduce `authorizationCheckDisabledPredicate` to make a denied export
work. Intermediate-installation public resources bypass normal resource checks;
never mark a private report `IsIntermediateInstallationPublicResource`.

## Verification

Exercise direct page and export requests as anonymous, allowed, and denied
users. Change a report/run identifier to another user's; ensure denied calls never
fetch protected data or emit file bytes. Where supported, test account deactivation
and revoked permissions with an existing cookie/session. Test operation-level
checks separately from menu visibility.
Pure policy tests are useful but do not establish routed HTTP enforcement.

## Verified source contracts

Checked against EWL source October 23, 2026; verify the consuming package too:

- `Core/EnterpriseWebFramework/Request Handling/ResourceBase.cs`: `UserCanAccess`,
  `userCanAccess`, `GetLogInPage`, authorization before `getOrHead`.
- `Core/UserManagement/SystemUser.cs`: identity/role; account-eligibility fields
  such as Active are system-owned, not framework properties.
- `Core/UserManagement/SystemUserManagementProvider.cs`: provider lookup contract.

Use `ewl-agent-testing` for behavioral evaluation. A built policy probe is not a
completed protected-page/export example; retain that distinction in results.
