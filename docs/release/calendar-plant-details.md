# Calendar names and personal plant links

Deployed on 2026-10-14 as `dbf65b828bc9a749675929ad30dc82a60f0b265a`.
All 12 frontend tests, the production build, local desktop care/calendar journey,
exact-candidate [CI run 38634142739](https://github.com/Odin2325/PlantCare/actions/runs/38634142739),
fresh encrypted pre-update backup and isolated runtime restore/rollback passed.
Pinned activation, restricted database access and public DNS/TLS/health passed.

The real public frontend upgrade from `cfbc073` to this candidate passed in
desktop Chromium with a dedicated account: the installed service worker offered
Update now, the old document stayed loaded before consent, new production main
and lazy chunks loaded afterward, and the authenticated session, collection IDs
and completed calendar history stayed intact. A new calendar link opened the
personal detail page and passed sampled axe checks. This used two actual builds,
not the same-build A/B fixture. All three public staging core journeys passed
after deployment, including another account's private-detail page denial.
The owner also confirmed the installed Android update, nickname/action links,
personal detail navigation and preserved plants/history. Follow-up feedback:
Care details looked like a link, so it is now styled as a non-underlined button;
it retains the event care panel while the primary event opens the plant page.

Calendar events display the individual plant nickname supplied by the calendar
API, together with the care action and scheduled/completed status. Nicknames are
required for saved plants. Month/week labels now wrap instead of truncating the
name/action. Agenda retains its time column.

In the owner's Month, Week and Agenda views, the event's primary link opens
`/my-plants/:id`: the saved individual plant, not a species catalog add form.
The authenticated detail page uses the existing owner-scoped plant API and shows
nickname (species fallback), species, location, acquisition date, tags, notes and
care schedules. Links lead to care history, Dashboard completion and species
information. Loading/errors have explicit states; the heading remains stable
while the data loads. No backend permissions or schema changed.

The separate Care details button keeps the existing event panel and eligible
care completion available. A shared calendar remains read-only: its event opens
the shared calendar panel and has no link into the owner's private plant page.
Guessing another user's personal plant URL still receives the existing API 404.

Frontend tests cover nickname precedence/species fallback, history link identity
and owner-scoped 404. Browser journeys check nickname/action and personal links
in all three calendar views, retained event details, linked personal details,
accessibility and another account's denial. Record actual candidate/results after
deployment; tests alone do not accept the installed Android upgrade.

This frontend change is also an opportunity for a real installed-app upgrade:
keep the previously installed app online, save unfinished work, accept Update now
when offered after deployment, then check the calendar links, account, collection
and history. Record the old/new candidates in android-beta.md. Do not reinstall
or clear storage as a substitute for upgrading the existing installation.
