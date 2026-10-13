# Keyboard/accessibility and guided beta acceptance

This is local UX-01 preparation, not a WCAG conformance statement. Automated
tools cannot establish complete accessibility; see [W3C evaluation guidance](https://www.w3.org/WAI/test-evaluate/tools/).

## Automated checks

```powershell
./scripts/Start-LocalDeployment.ps1
./scripts/Test-LocalBrowserJourney.ps1
```

The runner uses pinned `@axe-core/playwright` 4.13.0 with WCAG 2 A/AA, 2.1 A/AA
and 2.2 AA tags. No rules/regions are excluded. Any reported violation fails the
scan; only rule IDs, impact and node counts are printed. Raw HTML, input values,
full reports and secret URLs are withheld. Checks run on the three existing
desktop Chromium/mobile-sized Chromium/Firefox projects against real production
assets, real SQL/cookie authentication and captured local SMTP where applicable.

Public keyboard checks cover login, registration, forgot-password, resend,
incomplete reset-link and catalog pages at 320 CSS pixels. They verify one main
landmark, heading focus after routing, a keyboard-visible skip link, Enter to
main content, keyboard activation of navigation, a visible input focus ring,
inline email error association, no document-wide horizontal overflow and no
Content Security Policy violations. Horizontal scrolling inside a month-calendar
grid is distinct from document-wide overflow.

Scans also cover authenticated collection/schedule controls, catalog/details,
notification preferences, overdue dashboard, history, month/week/agenda calendar,
password-reset mismatch feedback and the PWA offline/update prompts. These are
representative states, not every possible dataset, error or browser extension.

Production improvements include a main/skip target per route, heading focus
after rendering (or main while loading), active navigation announcements, visible keyboard focus,
reduced-motion styling, announced form feedback, required/invalid field semantics,
associated instructions/errors, labelled care-action selection and higher-contrast
text. Disabling Angular critical-CSS inlining avoids its inline onload handler
being blocked by the existing script CSP. Script permissions were not relaxed.
Visiting notification preferences also exposed an uncaught Firefox subscription
lookup error. Disabled push no longer queries the browser; enabled lookup errors
show a controlled message and pending lookups are cancelled on page destruction.
See [W3C focus-visible guidance](https://www.w3.org/WAI/WCAG22/Understanding/focus-visible).

## Manual device and assistive-technology record

Record the candidate commit, exact OS/browser version, tester/date and result.
Use synthetic plants and dedicated test accounts; do not attach passwords,
cookies, reset links or raw browser traces to results.

| Environment | Native install | Keyboard/assistive technology | Status |
| --- | --- | --- | --- |
| Windows desktop Chromium | Install/standalone/update/uninstall | Keyboard-only; screen reader of choice | Manual NOT RUN |
| Windows Firefox | Web app and worker behavior | Keyboard-only; screen reader of choice | Manual NOT RUN |
| Physical Android Chromium | Install/standalone/update | Touch, zoom, screen-reader navigation | NOT RUN |
| Physical iPhone/iPad Safari, if advertised | Add to Home Screen/standalone/update | Touch, zoom, VoiceOver | NOT RUN |

Desktop browser automation and a mobile-sized viewport do not verify a physical
phone or screen-reader speech output. Current Docker ports are loopback-only;
`localhost` on a phone refers to that phone. Public physical-device beta needs
the staging host and trusted HTTPS domain, or a separately reviewed trusted LAN
setup. Do not expose SQL or weaken authentication/certificates to bypass this.

For each advertised environment:

1. Reach every primary action without a mouse. Confirm visible focus, sensible
   order, no traps, announced page changes, useful headings and the skip link.
2. With a screen reader, check navigation/current page, field names/required
   state, password requirements, invalid input, server errors and success messages.
   Exercise dynamic calendar selection and notification changes too.
3. Check 200% text zoom, 400% browser zoom/reflow, landscape/portrait, long plant
   names/tags, empty/loading/error states and high-contrast/reduced-motion settings.
4. Confirm the PWA offline explanation, failed writes retaining useful input,
   reconnect, consented update after saving work, install icon/name and standalone
   navigation. Upgrade from a separately built prior release on public HTTPS.
5. Test session expiry and loss of access while using a page; protected data must
   not become readable through cached content or another account.

Record failures in the release backlog with reproduction steps and a launch
decision. Fix blockers before accepting UX-01.

## Two normal care cycles

Begin once the tested environment remains stable. Use a synthetic daily schedule
or the normal cadence of a chosen test plant; do not change a real plant's care
solely to accelerate the software test. Do not change host clocks or mark future
care complete to simulate elapsed cycles.

| Evidence | Cycle 1 | Cycle 2 |
| --- | --- | --- |
| Actual due date/time and saved time zone | pending | pending |
| Reminder observed within the configured allowance | pending | pending |
| Completion saved once, visible in history and calendar | pending | pending |
| Next due date correct after reload/restart | pending | pending |
| No duplicate/misleading reminders | pending | pending |
| Device/browser, defects and tester/date | pending | pending |

Check account isolation with a second ordinary account. Real push permissions,
physical-device delivery, real calendar imports and optional provider integration
remain separate gates; synthetic provider tests do not accept them.

Collect feature ideas now, but keep v1 scope fixed until staging, operations,
device/PWA and two-cycle beta gates pass. Required v1 fixes remain appropriate;
start new feature implementation after those blockers are resolved.
