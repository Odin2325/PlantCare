# Public staging browser acceptance

## Verified on 2026-10-14

The real HTTPS staging runtime `7c78c9426aa423e32b980a6f195434840612e384`
passed the authenticated core journey in desktop Chromium, mobile-sized Chromium
and desktop Firefox (3/3). These are automated browser checks, not three physical
devices. Dedicated ordinary accounts A/B were explicitly approved by the owner;
their addresses/passwords and email links remain in restricted ignored local state.

Each browser passed catalog access, form-based plant creation with a past initial
care date, tag edit/reload, another-account read/history/complete/archive denials,
worker-generated reminder within the default polling allowance, dashboard care
completion, one history entry, completed agenda entry, archive/restore and logout.
Sampled axe checks covered catalog, collection, notification settings, history and
calendar. Signing B into the same browser after A logged out did not show A's
synthetic plant. Dedicated probe plants were archived; normal-owner plants were
not modified. These samples do not replace the entire v1 acceptance checklist.

The dedicated A account's browser forgot-password flow sent a real email through
Brevo and the message reached Gmail INBOX. Its validated private link was consumed
through the reset form. The test rejected the old password, the pre-reset cookie
session and reuse of the reset code; fresh login and logout passed. The normal
owner account/password was unchanged. Expired-reset, calendar-sharing, all schedule
editing and every cross-account endpoint are not established by this sample.

Earlier form-based registration and real confirmation of both approved aliases
passed on `d3d81f3`; public keyboard/reflow/axe checks passed in all three browser
projects there. Preserve those historical candidate labels. The owner reports
Android 17 / Chrome 155.0.8059.79 installation and app launch working. Push taps
now open the installed app but were reported to land on the catalog. The requested
direct dashboard launch remains a separate physical retest after deployment;
see [push-reliability.md](push-reliability.md). Two ordinary care cycles, physical
offline/reconnect/update checks and release packaging remain open.

## Runbook

Use PowerShell 7 and Docker Desktop/Linux containers. The default phase is a
read-only public-page check and keeps public certificate validation enabled:

```powershell
./scripts/Test-PublicBrowserAcceptance.ps1 -Phase Public
./scripts/Test-PublicBrowserAcceptance.ps1 -Phase Journey
# Targeted rerun, when a specific failure justifies it:
./scripts/Test-PublicBrowserAcceptance.ps1 -Phase Journey -Project desktop-firefox -SkipBuild
```

Run live browser checks separately from backups/deployment, which briefly stop
writers. Keep the default polling/worker thresholds. The runner verifies public
DNS contains the fixed VPS address before mapping that hostname inside Docker;
this avoids intermittent Docker DNS failures without bypassing HTTPS validation.
Tests use the real service worker and actual authenticated backend.

Non-public phases require an owner-only
`.operations-local/browser-acceptance/accounts.json`, with two distinct approved
recipients in `approvedRecipients`, `a`/`b` objects containing `email`, randomly
generated `password`, `created` and `confirmed`, and a separate random
`nextPassword`. Do not commit the file or reuse the owner's normal account.
Obtain explicit authorization for recipients before using mail-sending phases.
The runner restricts the directory ACL; operator preparation must do likewise.

`Register` and `Resend` send real confirmation messages; `Request-Reset` sends
a real password-reset message. They are deliberate operator phases, never CI
defaults. `Register` skips already-created accounts. Store only the selected
approved message's link privately as `confirmation-a.txt`/`confirmation-b.txt`
or `reset-a.txt`, without printing it. Then use:

```powershell
./scripts/Confirm-PublicBrowserAccount.ps1 -Account a
./scripts/Confirm-PublicBrowserAccount.ps1 -Account b
./scripts/Test-PublicBrowserAcceptance.ps1 -Phase Request-Reset -SkipBuild
./scripts/Resolve-PublicBrowserReset.ps1
./scripts/Test-PublicBrowserAcceptance.ps1 -Phase Reset -SkipBuild
```

The resolver allows trusted HTTPS links for the fixed staging host and bounded
Brevo tracking redirects, without forwarding app cookies. It verifies the final
route; the reset test also verifies the approved account query parameter. Used
link files are removed. A successful reset immediately saves the new private
password before subsequent assertions, then removes the consumed link. Generate
a new distinct `nextPassword` before repeating a reset.

Traces, screenshots and videos are disabled. The reporter suppresses raw runner
errors/stdout/stderr; only fixed stage/category codes and static test results
are emitted. Results are private JSON summaries. An earlier reporter exposed a
dedicated test session cookie; both dedicated sessions were revoked and the
reporter was replaced. No normal-owner session was revoked.

## Harness corrections

Initial attempts are failures, not acceptance evidence. A browser check was
started alongside backup and hit a login failure; later clean runs were serial.
Intermittent Docker DNS failures were addressed by the verified host mapping.
Firefox could discard a create-response body after navigation. A proposed route
interceptor also missed service-worker-owned requests, leaving no fixture ID and
causing false reminder timeouts. The final test resolves its unique fixture via
the authenticated collection API and asserts a real identifier before isolation,
polling or cleanup. The real worker remained enabled throughout the accepted run.
