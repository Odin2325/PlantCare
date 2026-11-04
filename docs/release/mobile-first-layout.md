# Mobile-first layout and navigation

Prepared 2026-11-03 after the shared visual refresh. This increment changes page
structure as well as appearance. Deployment acceptance is recorded separately;
source changes alone do not update the installed app or staging website.

## Design decisions

- Five primary destinations on a phone's bottom bar: Dashboard, My plants,
  Plant catalog, Calendar and More. Garden, Notifications, Account and logout are
  in More, which also highlights when a secondary destination is active. Tablet
  widths use a navigation rail; wide screens use header navigation. Labels and
  local vector icons stay visible. Native disclosure keyboard interaction, Escape,
  route-change closing and safe-area padding are retained.
- Catalog search stays visible. A Filters button reveals the category, cultivar,
  light, pet, photo, shortlist and sort controls. A visible count shows active
  advanced filters even while collapsed. URL filters, comparison selections and
  clear controls preserve their behavior. Shortlist/comparison instructions are
  secondary disclosures; cards put the photo/name first. Phone cards omit the
  long description, which remains on details, and keep care checks side by side.
- Notifications open on reminders. Preference/device configuration sits behind
  Reminder settings; snoozes use soft action chips, Review care opens Dashboard,
  and Mark as read remains a separate action. No reminder is completed by opening
  a page or changing its layout. Reminder history is paged in groups of twelve,
  preserving the loaded data and using explicit Previous/Next controls.
- The phone/tablet month grid fits its container. Selectable dates show event
  counts; a day agenda below shows full names, actions, status and detail controls.
  Week and Agenda remain available. Wide screens retain inline month events.
  Event filtering and sharing/integration setup are disclosures; important shared
  access status messages remain visible outside the disclosure.
- All pages inherit shared native controls, typography, focus, neutral surfaces
  and the botanical palette. Care, danger, completed and disabled states remain
  explicit. No remote font, UI library, new image dependency or paid service is
  introduced.

The navigation adapts the [Android layout/navigation guidance](https://developer.android.com/design/ui/mobile/guides/layout-and-content/layout-and-nav-patterns).
The search disclosure follows the mobile placement in the
[Scottish Government search-filter pattern](https://designsystem.gov.scot/patterns/search-results/search-filters).
These inform this application's implementation; their framework code is not copied.

## Verification and delivery gates

The combined candidate was deployed on 2026-11-04 after exact-candidate full CI
and fresh recovery acceptance. See [checkpoint evidence and limits](mobile-checkpoint-acceptance.md)
for live phone/tablet and existing-session update results. Photo uploads remain off.

The synthetic design audit checks phone (320px), tablet (768px) and desktop
(1440px) page families, expanded and collapsed filters/settings, keyboard
navigation, selected-day changes, empty days, and Month/Week/Agenda accessibility.
Its calendar fixture includes scheduled and completed entries. Background-color
animation was removed from buttons because view changes could briefly pair white
text with an intermediate pale background, failing contrast during the switch.

Existing catalog, sharing, account and shortlist browser journeys now open the
same menus/disclosures a person uses. They do not force hidden elements visible.
This UI work keeps both photo-processing/upload flags off. The accumulated
shortlist/journal migration batch still requires full candidate CI, restricted
runtime checks, migrated recovery and existing-session PWA update acceptance
before staging activation. Activating photo uploads has its own further gates.

Existing stylesheet-size warnings remain visible; budgets are not relaxed.

The first mobile candidate's combined browser run found incomplete disclosure
steps in the older journeys and a destination-render timing issue in their
helper. The helper now waits for the catalog page itself, and journeys explicitly
open the settings they use. A delayed catalog fixture checks the asynchronous
case. Real local acceptance also found an expanded calendar filter panel could
overlap event actions; filters now expand in page flow instead of overlaying them.
These fixes are checked locally before another combined candidate is pushed.
