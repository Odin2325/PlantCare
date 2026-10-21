# PlantCare privacy notice — review draft

Prepared 2026-10-21 for the staging service. This document is not yet the final
published service notice. The unresolved items in
[the privacy review](release/privacy-review.md) must be closed before publication
and wider signup. Operator details below were explicitly approved for publication.

## Who is responsible

PlantCare is operated by **Nicolas Arevalo Hoelscher, Munich, Germany**, as a
personal learning project. For private support and privacy requests, email
[nico.arevalo2325@gmail.com](mailto:nico.arevalo2325@gmail.com).
General support uses [GitHub Issues](https://github.com/Odin2325/PlantCare/issues),
which are public. Do not put private account information in an issue.

The current service is staging at https://staging.plantacare.org. Future
advertising is planned by the operator but is not implemented by the reviewed
application. This draft does not describe or authorize advertising processing.

## Information used and why

| Information | Purpose |
| --- | --- |
| Email address, account identifier, password hash and account security state | Create and protect your account, confirm your email and recover access. |
| Plants, nicknames, user-entered locations, notes, tags, schedules, time zones and care history | Display your collection, plan care and record completed actions. A location label is information you type; the application does not request device geolocation. |
| Reminder preferences, notifications, delivery records and optional device push subscription details | Show reminders and deliver browser push when enabled. Push services receive delivery information; reminder content can appear on your lock screen. |
| Private calendar subscription and sharing records | Supply optional read-only feeds and calendars you share with another account. Recipients see calendar entries; sharing also displays account email addresses to the participants. Anyone holding a private feed URL can use it until it is revoked. |
| Essential cookies and browser storage | Keep you signed in, protect requests, remember calendar view/device settings and support installation, updates and the offline application shell. |
| Technical errors and service-operation records | Diagnose faults, protect availability and operate recovery. Network services also process connection information such as IP addresses. |
| Private support correspondence | Answer questions and handle account and privacy requests. |

Passwords are handled by ASP.NET Core Identity. Account downloads exclude
password hashes, authentication tokens, push endpoints/keys and private feed
secrets. The download does not include all provider-held information or every
operational record; contact the operator for a broader access request.

**Legal-basis review still required before publication:** the proposed mapping is
Article 6(1)(b) GDPR for requested account and plant-care services, Article 6(1)(f)
for proportionate security, reliability and erasure-suppression measures, and
Article 6(1)(c) where processing is necessary to fulfil legal duties. The operator
must assess necessity and the legitimate-interest balance, and separately assess
device-storage/push permissions and any consent requirements. Browser notification
permission is not blanket consent to unrelated processing.

## Cookies, storage and choices

Authentication and antiforgery cookies support account access and request
protection. Authentication has a seven-day sliding lifetime: continued use can
renew it. Calendar-view and push-device identifiers use local storage. The service
worker caches application assets, not an offline copy of your private plant API
responses. Browser storage and installed-app data are separate from server data.

You can disable push in PlantCare and your browser, revoke private feeds and
shares in Calendar, sign out, and clear site data using browser settings. Clearing
site data may require signing in again. A notification already in delivery may
still arrive. Copies held by calendar clients or recipients may remain after
revocation.

No advertising or analytics SDK was found in the reviewed application. This is
not a statement that hosting, email, browser or other providers collect no
operational information. Google/Microsoft calendar synchronization remains
disabled in the current staging setup.

## Services and recipients

The current application/database host is **OVHcloud in the UK**. **Brevo** sends
confirmation and password-reset emails and processes recipient addresses, message
content and delivery events. Optional web push uses your browser's push service.
External calendar services receive data when you subscribe or share. **Google
Gmail** receives private requests sent to the published address; **GitHub** hosts
public support issues. UptimeRobot checks public service-health endpoints, and
owner-only backup alert emails support operations.

**Complete before publication:** identify the contracted provider entities,
applicable processor agreements, processing locations, subprocessors and transfer
mechanisms. Do not infer that all processing stays in Germany or the EU from
the operator's location. Brevo tracking settings must be verified; application
code alone does not prevent provider-added tracking links or pixels.

## How long information remains

Account information and associated plant/history/reminder records remain until
account deletion, unless you remove individual supported records earlier.
Archiving a plant preserves it. Account deletion removes active account data and
revokes access. A minimal account-ID/deletion-time marker remains to prevent a
later restore from making a deleted account active again; marker expiry is not
currently implemented.

Encrypted application backups target 30-day expiry after verified backups.
Unavailable storage or failed safety checks can delay removal, so 30 days is not
a guaranteed maximum. Managed application recovery reconciles current account and
deletion authority before opening access. The service cannot use an old backup
as authority for reopening deleted accounts. Downloaded exports, delivered
messages, external calendar copies and separately retained recovery artifacts
are not all covered by the encrypted-repository expiry rule.

The operator reports configuring Brevo transactional logs for one month, with no
previews stored for future messages, on 2026-10-21. The log rule also applies to
existing logs; disabling previews does not remove old previews. Provider deletion
is asynchronous. Actual expiry and tracking behavior remain to be verified.
Application/container logs, support correspondence and erasure markers do not
yet have verified automatic time-based expiry.
Container console logs have size-based rotation (three 10 MB files per container),
which limits disk use but does not establish a maximum age.
Resolve their retention criteria in the operator review before publication.

## Your requests and rights

Use Account for a password-verified download or account deletion. You can also
contact the private email above about access, correction, erasure, restriction,
portability or objection, as applicable. Where processing relies on consent, you
can withdraw it without affecting the lawfulness of earlier processing. Identity
may need to be verified before private information is disclosed or changed. Never
send a password, reset link or private calendar URL as proof.

Requests should be handled without undue delay, normally within one month;
applicable exceptions and any extension must be explained. You can complain to a
data-protection supervisory authority, including where you live or work. For a
private-sector operator in Bavaria, the relevant local authority is the
[Bavarian State Office for Data Protection Supervision (BayLDA)](https://www.lda.bayern.de/de/beschwerde.html).

The reviewed application does not perform automated decisions with legal or
similarly significant effects. Providing account credentials is necessary to use
account features; optional push and calendar sharing are not required to manage
plants.

## Changes

Before advertising, new analytics, new recipients or materially different data
uses are enabled, review the processing, provide updated information and implement
any required choices. Registering for PlantCare must not be treated as consent to
future advertising tracking.
