# Password resets and browser-session security

With Docker Desktop running Linux containers, run in PowerShell 7:

```powershell
./scripts/Start-LocalDeployment.ps1
./scripts/Test-LocalAuthenticationSecurity.ps1
```

The script uses only the local HTTPS app at `localhost:8443`, Mailpit at
`localhost:8025`, and synthetic accounts in Docker SQL Server. Windows SQL Server
is untouched. Passwords, confirmation links, reset codes, and cookie values are
not printed. Certificate verification is skipped only for the fixed local origin.
Run it separately from other acceptance/recovery scripts. Its bounded waits
respect the production authentication rate limit; a run can take several minutes.

## Executed SQL Server checks

- A confirmed account can log in from two separate cookie jars; an unconfirmed
  account is rejected. Authentication cookies use Secure, HttpOnly, and SameSite=Lax.
- Unsafe cookie requests reject missing, invalid, and anonymous-identity
  antiforgery tokens. A valid token works. A bogus Bearer header does not bypass
  protection on controllers or anonymous Identity endpoints.
- Forgot-password returns the same status/body for confirmed, unknown, and
  unconfirmed accounts. Only the confirmed account receives a reset email.
  This verifies response content, not resistance to statistical timing analysis.
- The delivered reset URL targets the configured HTTPS reset page and contains
  usable email/code parameters. The Angular shell is served there; browser form
  interactions remain a separate acceptance check.
- Invalid codes, wrong-account requests, weak passwords, and reuse after success
  are rejected. A successful reset invalidates both existing browser sessions
  on their next request. The old password fails and the new one succeeds.
- Protected logout clears the current browser session and protected reads fail.

Synthetic accounts and captured messages remain in the isolated local database
and Mailpit. The unconfirmed fixture stays unconfirmed. There are no host clock
changes, production credentials, or external email deliveries.

## Implementation and regression coverage

Antiforgery exemptions require successful authentication with the actual Identity
bearer scheme. Merely providing an Authorization header is insufficient. Valid
bearer API requests continue to work without cookie antiforgery tokens.

Cookie security stamps are validated on every request (`ValidationInterval = 0`),
so password resets and other stamp changes take effect immediately. This adds
Identity database validation to authenticated cookie requests; account for that
load when sizing/monitoring a production host. The existing seven-day sliding
cookie lifetime is unchanged.

The API integration suite can now use the real Identity/cookie/bearer pipeline
instead of its simulated-user handler. Its dedicated tests cover the bogus-header
bypass, valid bearer requests, two-cookie invalidation after reset, old refresh-token
rejection, reset-code reuse, a code applied to another confirmed account, expired
reset codes, and expired authentication cookies. Expiry is tested using test-only
zero token lifetime and a properly protected expired cookie ticket, without
changing production expiry settings or advancing the system clock.

Both the SQLite real-Identity tests and the SQL/SMTP script run in GitHub CI.
The SQL script runs after account isolation against the same disposable deployment.

## Scope of session revocation

Password reset invalidates browser cookies through the Identity security stamp;
refresh tokens with the old stamp are also rejected. Already-issued bearer access
tokens can remain valid until their configured expiry. The current web application
uses cookie authentication; immediate bearer-access-token revocation is not added.

Normal logout clears the current browser's cookie. It does not revoke other devices
or add a server-side ticket store to invalidate copied cookie tickets. Password
reset does invalidate those older cookie tickets. Per-device server-side session
management remains a separate enhancement if required for launch.

Framework behavior is documented in the [Identity endpoint implementation](https://github.com/dotnet/aspnetcore/blob/v10.0.10/src/Identity/Core/src/IdentityApiEndpointRouteBuilderExtensions.cs)
and [security-stamp validator](https://github.com/dotnet/aspnetcore/blob/v10.0.10/src/Identity/Core/src/SecurityStampValidator.cs).
