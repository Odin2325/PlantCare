# Using PlantCare

PlantCare helps you keep a personal collection, schedule care and record what
you have done. The current environment is a staging beta at
<https://staging.plantacare.org>, not a production release. Use synthetic test
data while the launch requirements remain open. The catalog's current demo
entry is not reviewed botanical guidance.

## Your first plant

1. Create an account and use the confirmation link sent to your email address.
   Check spam if it does not arrive; the sign-in flow also offers confirmation
   resend and password recovery. Keep email links private.
2. Sign in, browse the plant catalog and open a species. Add it to My plants.
3. Give the individual plant a nickname. Add a location, tags and notes if useful.
   Avoid sensitive personal information in these fields.
4. Review the initial care dates and schedules. Choose intervals or weekdays as
   appropriate, and check the saved time zone and displayed next-due time.
   Suggested intervals are starting points; actual needs depend on the plant
   and its conditions.
5. Find the plant in My plants using search, location/tag filters or sorting.

## Daily care and calendar

Open Dashboard to review due care and record an action when performed. Verify
the resulting care history and next-due date. A notification is a reminder;
opening it or marking it read does not record the care action.

Calendar offers Month, Week and Agenda views and plant/action/status filters.
An event shows the plant nickname and care action. In your own calendar:

- The main event link opens that individual plant's detail page.
- Care details opens the event panel; eligible due care can be completed there.
- The personal detail page links to care history, Dashboard and species details.

Shared calendars are read-only. Their entries open the shared event panel and
do not grant access to the owner's private plant detail page.

Archiving a plant hides it from the active collection and stops new care
reminders. Use Show archived to find it and restore it. Archiving is not erasure.

## Reminders

In Notifications, choose in-app and push delivery, care actions, and reminder
lead time, then choose Save preferences. To receive push on a particular device,
also choose Enable push reminders there and allow the browser permission.
Changing a global preference does not grant permission on a new device.

A newly received care push opens Dashboard. Browser permissions, device battery
settings, network access and the push provider can delay or prevent delivery.
Review Dashboard even if no push arrives. Disable on this device removes that
device's subscription; browser/site settings also control notification permission.

## Install on Android and update

Open the HTTPS staging site in Chrome. Use Chrome's menu to install the app;
depending on the browser version this may be Install app or Add to Home screen
followed by Install. Open the resulting PlantCare icon to use the standalone app.
If the option is missing, return online, reload, and check whether it is already
installed. See [support](../SUPPORT.md) if it remains unavailable.

When PlantCare offers Update now, save unfinished input and accept the update.
It reloads the app with the new version. A routine update does not require
reinstallation or clearing site storage. Those actions can remove local settings
and push permission/subscriptions, so do not use them as the first troubleshooting
step. Server-side data remains associated with your account.

If opened offline, the app shows an offline explanation. Offline care entry and
full offline plant access are not supported. Return online and reload/reopen;
sign in if requested, then check the current collection and care history.

## Calendar feeds and sharing

Calendar can generate a private read-only subscription URL. Treat the entire URL
like a password: anyone who has it can read the feed. Save it when generated;
PlantCare cannot show the original URL again. Regenerate or revoke it if exposed.
An external calendar may retain previously downloaded entries after revocation.
External calendar clients refresh on their own schedule.

You can also share your calendar with another registered PlantCare account and
revoke that access later. Check the intended recipient before sharing. Real
calendar-client import/revocation acceptance remains a release check.
Google and Microsoft account synchronization are disabled in this staging setup
and are not part of the current launch commitment.

## Your account and data

The new Account page offers Download my data and Permanently delete my account.
Enter your current password for either action; deletion additionally requires
typing DELETE. Export first if you want a private copy. A successful deletion
signs you out and removes active account/plant/care records. Connected external
calendars must be disconnected first. Existing encrypted backups, delivered
messages and copies held by calendar clients are separate. Backup cleanup targets
30 days but can be delayed if storage is unavailable or verification fails.
A minimal deletion marker is kept to prevent restoration during managed recovery.
See [deployment/enabling status](release/retention-and-erasure-recovery.md).

## Getting help

Use [GitHub Issues for general support](../SUPPORT.md). Never post a password,
reset/confirmation link or calendar-feed URL there. See
[known limitations](known-limitations.md) for the supported beta scope.
For private account/privacy requests, email
[nico.arevalo2325@gmail.com](mailto:nico.arevalo2325@gmail.com).
