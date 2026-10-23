# Custom externally validated login

Keep external credential validation separate from local EWL identity/authorization.
Use the normal EWL form/postback flow, obscured password control, and
`AuthenticationStatics.GetLogInHiddenFieldsAndMethods()`. Add its `hiddenFields`
to the page and call `modificationMethods.specifiedUserLoginMethod( userId )`
only after fresh external validation, identity mapping, and local account-eligibility checks.
Do not call local password/code methods when no local identity provider is enabled.
Do not construct authentication cookies yourself or trust a posted local user ID.

The method establishes EWL authentication; it does not validate external credentials
or enforce system-defined eligibility rules, such as an active-account check.
Its optional authentication duration is sliding,
not an absolute maximum login lifetime. Keep passwords out of logs/cache/storage.
Local-provider password rate limiting is not automatically applied to a custom
external-login handler; choose bounded attempts appropriate to the upstream service.

## Provider and schema

Implement the real `SystemUserManagementProvider` contract, including request
tracking methods. `GetUser(string)` requires case-insensitive email lookup for
passwordless login/impersonation; this is not permission to link external identities
by email. Resolve external identities by stable UID qualified by configured target.
Inspect email uniqueness/missing-email behavior and the built-in admin UI before
enabling it for external-only accounts. Its generic insertion method cannot supply
an external UID; do not silently fabricate one or create an alternate password account.

If the system supports account deactivation or other eligibility restrictions,
enforce them on protected resources/operations even if an old EWL cookie survives.
Define provider lookup and authorization together rather than assuming `SystemUser`
has an Active property. Use `ewl-authorization` for resource
policy and `ewl-external-integrations` for the separate external-session lifecycle.

Define the application's external identity fields, approved targets, eligibility,
first-login provisioning, initial privileges, and reactivation policy explicitly.
Enforce unique identity mapping transactionally for simultaneous first logins.
Do not overwrite existing roles/status on login unless the application's approved
policy calls for it. Target changes require explicit remapping/reset, not numeric
UID reuse. Keep concrete schema and provisioning decisions in a system-owned skill.

## Routing and verification

`ResourceBase.GetLogInPage(TrustedUrl)` can select the custom login page and inherits
from the parent by default. Verify actual routing and return-URL handling with
`ewl-page`; do not blindly copy an authorization bypass from another system.
The development Select User page is not evidence that external authentication works.
Test the custom page itself, including fresh invalid credentials despite an existing
cached session, ineligible external accounts, local eligibility restrictions where
supported, concurrent creation, email change with stable UID, target change, and the
chosen role-update policy.

Verified EWL source October 23, 2026:
`Core/UserManagement/SystemUserManagementProvider.cs`,
`Core/EnterpriseWebFramework/UserManagement/AuthenticationStatics.cs` and
`Core/EnterpriseWebFramework/Request Handling/ResourceBase.cs`.
